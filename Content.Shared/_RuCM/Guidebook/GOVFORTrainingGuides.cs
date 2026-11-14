namespace Content.Shared._RuCM.Guidebook;

/// <summary>Public reading references only; this mapping does not change training requirements or saved progress.</summary>
public static class GOVFORTrainingGuides
{
    public const string Root = "RuCMGOVFOR";
    public const string Basic = "RuCMGOVFORBasicDocuments";
    public const string Service = "RuCMGOVFORService";
    public const string Training = "RuCMGOVFORTraining";
    public const string Drill = "RuCMGOVFORDrillRegulations";
    public const string Communications = "RuCMGOVFORCommunications";
    public const string Command = "RuCMGOVFORUnitCommand";
    public const string Qualifications = "RuCMGOVFORTrainingQualifications";
    public const string Sop = "AU14SOP";
    public const string MilitaryCode = "AU14UCMJ";
    public const string RecruitCourse = "RuCMGOVFORRecruitCourse";
    public const string RecruitIntroduction = RecruitCourse + "Introduction";
    public const string RecruitMilitaryRegulations = RecruitCourse + "MilitaryRegulations";
    public const string RecruitDrill = RecruitCourse + "Drill";
    public const string RecruitCommunications = RecruitCourse + "Communications";
    public const string RecruitFirearms = RecruitCourse + "Firearms";
    public const string RecruitTactics = RecruitCourse + "Tactics";
    public const string RecruitFirstAid = RecruitCourse + "FirstAid";
    public const string RecruitTopography = RecruitCourse + "Topography";
    public const string RecruitEngineering = RecruitCourse + "EngineeringBasics";
    public const string RecruitFieldExercise = RecruitCourse + "FieldExercise";
    public const string RecruitReference = RecruitCourse + "Reference";

    public const string InstructorHandbook = "RuCMGOVFORInstructorHandbook";
    public const string InstructorWork = InstructorHandbook + "Work";
    public const string InstructorLesson = InstructorHandbook + "Lesson";
    public const string InstructorAssessment = InstructorHandbook + "Assessment";
    public const string InstructorMilitaryRegulations = InstructorHandbook + "MilitaryRegulations";
    public const string InstructorDrill = InstructorHandbook + "Drill";
    public const string InstructorCommunications = InstructorHandbook + "Communications";
    public const string InstructorFirearms = InstructorHandbook + "Firearms";
    public const string InstructorTactics = InstructorHandbook + "Tactics";
    public const string InstructorFirstAid = InstructorHandbook + "FirstAid";
    public const string InstructorTopography = InstructorHandbook + "Topography";
    public const string InstructorEngineeringBasics = InstructorHandbook + "EngineeringBasics";
    public const string InstructorFieldExercise = InstructorHandbook + "FieldExercise";
    public const string InstructorGroup = InstructorHandbook + "Group";
    public const string InstructorCertification = InstructorHandbook + "Certification";
    public const string InstructorReference = InstructorHandbook + "Reference";

    public const string SergeantCourse = "RuCMGOVFORSergeantCourse";
    public const string SergeantAssessment = "RuCMGOVFORSergeantAssessment";
    public const string OfficerCourse = "RuCMGOVFOROfficerCourse";
    public const string OfficerAssessment = "RuCMGOVFOROfficerAssessment";
    public const string CourseMedical = "RuCMGOVFORCourseMedical";
    public const string AssessmentMedical = "RuCMGOVFORAssessmentMedical";
    public const string CourseFieldEngineering = "RuCMGOVFORCourseFieldEngineering";
    public const string AssessmentFieldEngineering = "RuCMGOVFORAssessmentFieldEngineering";
    public const string CourseTechnicalEngineering = "RuCMGOVFORCourseTechnicalEngineering";
    public const string AssessmentTechnicalEngineering = "RuCMGOVFORAssessmentTechnicalEngineering";
    public const string CourseLogistics = "RuCMGOVFORCourseLogistics";
    public const string AssessmentLogistics = "RuCMGOVFORAssessmentLogistics";
    public const string CourseHeavyWeapons = "RuCMGOVFORCourseHeavyWeapons";
    public const string AssessmentHeavyWeapons = "RuCMGOVFORAssessmentHeavyWeapons";
    public const string CourseProfessionalCommunications = "RuCMGOVFORCourseProfessionalCommunications";
    public const string AssessmentProfessionalCommunications = "RuCMGOVFORAssessmentProfessionalCommunications";
    public const string CourseAviation = "RuCMGOVFORCourseAviation";
    public const string AssessmentAviation = "RuCMGOVFORAssessmentAviation";
    public const string CourseVehicleCrew = "RuCMGOVFORCourseVehicleCrew";
    public const string AssessmentVehicleCrew = "RuCMGOVFORAssessmentVehicleCrew";
    public const string CourseDroneOperator = "RuCMGOVFORCourseDroneOperator";
    public const string AssessmentDroneOperator = "RuCMGOVFORAssessmentDroneOperator";
    public const string CourseMilitaryPolice = "RuCMGOVFORCourseMilitaryPolice";
    public const string AssessmentMilitaryPolice = "RuCMGOVFORAssessmentMilitaryPolice";
    public const string CourseIntelligence = "RuCMGOVFORCourseIntelligence";
    public const string AssessmentIntelligence = "RuCMGOVFORAssessmentIntelligence";
    public const string CommandingOfficerCourse = "RuCMGOVFORCommandingOfficerCourse";
    public const string CommandingOfficerInterview = "RuCMGOVFORCommandingOfficerInterview";
    public const string ProfessionalTraining = "RuCMGOVFORProfessionalTraining";
    public const string ProfessionalAssessments = "RuCMGOVFORProfessionalAssessments";

    public static bool IsReference(string id) => id is Root or Basic or Service or Training or Drill
        or Communications or Command or Qualifications or Sop or MilitaryCode or RecruitCourse or RecruitIntroduction
        or RecruitMilitaryRegulations or RecruitDrill or RecruitCommunications or RecruitFirearms or RecruitTactics
        or RecruitFirstAid or RecruitTopography or RecruitEngineering or RecruitFieldExercise or RecruitReference or InstructorHandbook
        or InstructorWork or InstructorLesson or InstructorAssessment or InstructorMilitaryRegulations
        or InstructorDrill or InstructorCommunications or InstructorFirearms or InstructorTactics
        or InstructorFirstAid or InstructorTopography or InstructorEngineeringBasics or InstructorFieldExercise
        or InstructorGroup or InstructorCertification or InstructorReference
        or SergeantCourse or SergeantAssessment or OfficerCourse or OfficerAssessment
        or CourseMedical or AssessmentMedical or CourseFieldEngineering or AssessmentFieldEngineering
        or CourseTechnicalEngineering or AssessmentTechnicalEngineering or CourseLogistics or AssessmentLogistics
        or CourseHeavyWeapons or AssessmentHeavyWeapons or CourseProfessionalCommunications or AssessmentProfessionalCommunications
        or CourseAviation or AssessmentAviation or CourseVehicleCrew or AssessmentVehicleCrew
        or CourseDroneOperator or AssessmentDroneOperator or CourseMilitaryPolice or AssessmentMilitaryPolice
        or CourseIntelligence or AssessmentIntelligence or CommandingOfficerCourse or CommandingOfficerInterview
        or ProfessionalTraining or ProfessionalAssessments
        or "AU14Comms" or "AU14SOPDropshipsandAircraft" or "AU14SOPVehicles" or "CMUGuideEngFortify"
        or "CMUGuideEngPower" or "CMUGuideEngVehicles" or "CMUGuideEngineering" or "CMUGuideMedical"
        or "CMUGuideMedicalSurgery" or "CMUGuideMortar" or "CMUGuideTowers" or "RMCGuideRoleMilitaryPolice"
        or "RMCGuideRoleSpec" or "RMCIntel" or "RMCRequisitions";

    /// <summary>Uses existing stable IDs, while also supporting the equivalent IDs in custom training programmes.</summary>
    public static string[] ForChecklistItem(string qualification, string item) => (qualification, item) switch
    {
        ("enlisted", "drill") => [Drill, RecruitDrill],
        ("enlisted", "communications") => [Communications, RecruitCommunications],
        ("enlisted", "military_regulations") => [Sop, MilitaryCode, Drill, RecruitMilitaryRegulations],
        ("enlisted", "firearms") => [RecruitFirearms],
        ("enlisted", "tactics") => [RecruitTactics],
        ("enlisted", "first_aid") => [RecruitFirstAid],
        ("enlisted", "topography") => [RecruitTopography],
        ("enlisted", "engineering_basics") => [RecruitEngineering],
        ("enlisted", "field_exercise") => [RecruitFieldExercise],
        ("squad_leader", "military_regulations") => [Sop, MilitaryCode], // CMU14
        ("squad_leader", "tactics" or "unit_command") => [Command, SergeantAssessment], // CMU14
        ("sergeant", "drill") => [SergeantCourse, SergeantAssessment, Drill],
        ("sergeant", "military_regulations") => [SergeantCourse, SergeantAssessment, Sop, MilitaryCode],
        ("sergeant", "teaching") or ("sergeant", "methodical_training") => [SergeantCourse, SergeantAssessment, InstructorHandbook],
        ("sergeant", "tactics") or ("sergeant", "firearms") or ("sergeant", "topography")
            or ("sergeant", "unit_command") or ("sergeant", "unit_management") => [SergeantCourse, SergeantAssessment, Command],
        ("officer", "drill") => [OfficerCourse, OfficerAssessment, Drill],
        ("officer", "military_regulations") => [OfficerCourse, OfficerAssessment, Sop, MilitaryCode],
        ("officer", "tactics") or ("officer", "firearms") or ("officer", "operations")
            or ("officer", "operational_management") or ("officer", "staff_work")
            or ("officer", "crisis_management") => [OfficerCourse, OfficerAssessment, Command],
        ("medical", "theory" or "safety" or "practice") => [CourseMedical, AssessmentMedical],
        ("field_engineering", "theory" or "safety" or "practice") => [CourseFieldEngineering, AssessmentFieldEngineering],
        ("technical_engineering", "theory" or "safety" or "practice") => [CourseTechnicalEngineering, AssessmentTechnicalEngineering],
        ("logistics", "theory" or "safety" or "practice") => [CourseLogistics, AssessmentLogistics],
        ("heavy_weapons", "theory" or "safety" or "practice") => [CourseHeavyWeapons, AssessmentHeavyWeapons],
        ("communications", "theory" or "safety" or "practice") => [CourseProfessionalCommunications, AssessmentProfessionalCommunications],
        ("aviation", "theory" or "safety" or "practice") => [CourseAviation, AssessmentAviation],
        ("vehicle_crew", "theory" or "safety" or "practice") => [CourseVehicleCrew, AssessmentVehicleCrew],
        ("drone_operator", "theory" or "safety" or "practice") => [CourseDroneOperator, AssessmentDroneOperator],
        ("military_police", "theory" or "safety" or "practice") => [CourseMilitaryPolice, AssessmentMilitaryPolice],
        ("intelligence", "theory" or "safety" or "practice") => [CourseIntelligence, AssessmentIntelligence],
        ("commanding_officer", "theory" or "safety" or "practice") => [CommandingOfficerCourse, CommandingOfficerInterview],
        _ => [],
    };
}
