import renderdoc as rd
import os, sys, json, csv, traceback, struct, math
root = os.path.join(os.getcwd(), 'bin', 'cinematic-demo')
def shader_role(text):
    for token, role in [('face_luminous','wall-receiver'),('contact_strength','contact-mask'),('core_strength','emission'),('spill_strength','spill'),('atmosphere_strength','atmosphere'),('bloom_compact','lens-composite'),('reflection_texture','surface-composite'),('irradiance_texture','native-light-composite'),('native_copy_texture','native-light-copy'),('source_pixel','bloom')]:
        if token in text:
            return role
    return None
def uf(bits, mantissa):
    exponent, fraction = bits >> mantissa, bits & ((1 << mantissa)-1)
    if exponent == 0: return fraction * 2.0**(1-15-mantissa)
    if exponent == 31: return float('inf') if fraction == 0 else float('nan')
    return (1.0 + fraction / float(1 << mantissa)) * 2.0**(exponent-15)
with open(os.path.join(root,'gpu-final-analysis.log'),'w',encoding='utf-8') as log:
    try:
        files = json.load(open(os.path.join(root,'gpu-preset-files.json')))
        samples, details, hdr = [], [], []
        for number, item in enumerate(files):
            cap = rd.OpenCaptureFile()
            result = cap.OpenFile(item['path'],'',None)
            result, c = cap.OpenCapture(rd.ReplayOptions(),None)
            if c is None: raise RuntimeError(str(result))
            sf = c.GetStructuredFile()
            names = {str(r.resourceId):r.name for r in c.GetResources()}
            roles, parents = {}, {}
            last_event = [0]
            def walk(actions, path):
                for a in actions:
                    last_event[0] = max(last_event[0],a.eventId)
                    parents[a.eventId] = '/'.join(path)
                    if a.flags & (rd.ActionFlags.Drawcall | rd.ActionFlags.Clear):
                        c.SetFrameEvent(a.eventId,True)
                        pipe = c.GetPipelineState()
                        if a.flags & rd.ActionFlags.Drawcall:
                            s = pipe.GetShaderReflection(rd.ShaderStage.Fragment)
                            text = '\n'.join(f.contents for f in s.debugInfo.files) if s else ''
                            role = shader_role(text)
                            outputs = [names.get(str(t.resource),'') for t in pipe.GetOutputTargets()]
                            if role == 'spill':
                                for label, effect in [('irradiance','direct-indirect'),('reflections','specular'),('native-contribution','native-contribution')]:
                                    if any(label in name for name in outputs): role = effect
                            if role == 'emission':
                                for label, effect in [('inner-seeds','emission-inner'),('outer-seeds','emission-outer')]:
                                    if any(label in name for name in outputs): role = effect
                            if not role:
                                for t in pipe.GetOutputTargets():
                                    name = names.get(str(t.resource),'')
                                    if 'cmu-cinematic-materials' in name: role = 'material-mask'
                                    if 'cmu-cinematic-blockers' in name: role = 'blocker-mask'
                            if role: roles[a.eventId] = role
                        else:
                            outputs = pipe.GetOutputTargets()
                            if any('cmu-cinematic' in names.get(str(t.resource),'') for t in outputs):
                                roles[a.eventId] = 'field-clear'
                    walk(a.children,path+[a.GetName(sf)])
            walk(c.GetRootActions(),[])
            # Replay once before timing so a fresh capture's driver/pipeline warmup is not
            # folded into the first set of event durations. Keep every measured sample.
            c.FetchCounters([rd.GPUCounter.EventGPUDuration])
            counters = c.FetchCounters([rd.GPUCounter.EventGPUDuration])
            total, own = 0.0, 0.0
            for value in counters:
                ms = value.value.d * 1000.0
                total += ms
                role = roles.get(value.eventId,'native')
                if role != 'native': own += ms
                details.append([item['quality'], number, value.eventId,role,ms,parents.get(value.eventId,'').rstrip()])
            samples.append({'quality':item['quality'],'sample':number,'event_gpu_ms':total,'cmu_draw_clear_gpu_ms':own,'cmu_events':len(roles)})
            log.write(str(samples[-1])+'\n'); log.flush()
            if item['quality']==2 and not hdr:
                c.SetFrameEvent(last_event[0],True)
                for tex in c.GetTextures():
                    name = names.get(str(tex.resourceId),'')
                    if any(s in name for s in ['cmu-cinematic-emission','cmu-cinematic-inner-seeds','cmu-cinematic-outer-seeds','cmu-cinematic-bloom-','cmu-cinematic-irradiance','cmu-cinematic-reflections','cmu-cinematic-atmosphere','cmu-cinematic-native-contribution','cmu-cinematic-native-light-copy']):
                        data = c.GetTextureData(tex.resourceId,rd.Subresource())
                        peaks=[0.0,0.0,0.0]
                        nonfinite=0
                        for value, in struct.iter_unpack('<I',data):
                            rgb = [uf(value&2047,6),uf((value>>11)&2047,6),uf((value>>22)&1023,5)]
                            nonfinite += sum(not math.isfinite(x) for x in rgb)
                            peaks = [max(peaks[i],rgb[i]) for i in range(3)]
                        hdr.append({'target':name,'format':tex.format.Name(),'width':tex.width,'height':tex.height,'rgb_max':peaks,'nonfinite_channels':nonfinite})
            c.Shutdown(); cap.Shutdown()
        with open(os.path.join(root,'gpu-final-samples.json'),'w') as output: json.dump({'samples':samples,'hdr':hdr},output,indent=2)
        with open(os.path.join(root,'gpu-final-events.csv'),'w',newline='',encoding='utf-8') as output:
            writer=csv.writer(output); writer.writerow(['quality','sample','event','role','milliseconds','scope']); writer.writerows(details)
    except Exception:
        log.write(traceback.format_exc())
sys.exit(0)
