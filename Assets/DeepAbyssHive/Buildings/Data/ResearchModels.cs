namespace DeepAbyssHive.Buildings.Data
{
    public struct ResearchPrerequisiteResult
    {
        public bool Ok;
        public string Reason;
        public static ResearchPrerequisiteResult Success() => new ResearchPrerequisiteResult { Ok = true, Reason = null };
        public static ResearchPrerequisiteResult Fail(string reason) => new ResearchPrerequisiteResult { Ok = false, Reason = reason };
    }

    public struct ResearchUnlocks
    {
        // 可依實際需要擴充；先提供最小可編譯結構
        public string[] Abilities;
        public string[] Buildings;
        public string[] Units;
    }
}