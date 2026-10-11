using System;
using System.Threading.Tasks;
using Content.Server.Corvax.TTS;
using NUnit.Framework;

namespace Content.Tests.CMU14.TTS;

[TestFixture]
public sealed class CMUTTSRequestRunnerTest
{
    [Test]
    public async Task FailureBeforeFirstAwaitIsReportedWithoutEscaping()
    {
        var failure = new InvalidOperationException("request setup failed");
        Exception observed = null;
        await CMUTTSRequestRunner.Run(() => throw failure, e => observed = e);
        Assert.That(observed, Is.SameAs(failure));
    }

    [Test]
    public async Task DeliveryFailureAfterAwaitIsReportedAndFinallyStillRuns()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("audio delivery failed");
        Exception observed = null;
        var pending = true;
        async Task Request()
        {
            try
            {
                await completion.Task;
                throw failure;
            }
            finally
            {
                pending = false;
            }
        }
        var request = CMUTTSRequestRunner.Run(Request, e => observed = e);
        Assert.That(request.IsCompleted, Is.False);
        completion.SetResult();
        await request;
        Assert.That(observed, Is.SameAs(failure));
        Assert.That(pending, Is.False);
        var subsequentCompleted = false;
        await CMUTTSRequestRunner.Run(() =>
        {
            subsequentCompleted = true;
            return Task.CompletedTask;
        }, e => Assert.Fail(e.ToString()));
        Assert.That(subsequentCompleted, Is.True);
    }

    [Test]
    public async Task CancellationDoesNotReportAnError()
    {
        await CMUTTSRequestRunner.Run(() => Task.FromException(new OperationCanceledException()),
            e => Assert.Fail(e.ToString()));
    }

    [Test]
    public async Task SuccessfulRequestDoesNotReportAnError()
    {
        await CMUTTSRequestRunner.Run(() => Task.CompletedTask, e => Assert.Fail(e.ToString()));
    }
}
