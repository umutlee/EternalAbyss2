using System;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>Result of checking whether a research can start.</summary>
    public class ResearchPrerequisiteResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public string[] MissingPrerequisites { get; set; }
        public string[] MissingBuildings { get; set; }

        public static ResearchPrerequisiteResult Success() =>
            new ResearchPrerequisiteResult
            {
                IsValid = true,
                ErrorMessage = string.Empty,
                MissingPrerequisites = Array.Empty<string>(),
                MissingBuildings = Array.Empty<string>()
            };

        public static ResearchPrerequisiteResult Failure(
            string errorMessage,
            string[] missingPrerequisites = null,
            string[] missingBuildings = null) =>
            new ResearchPrerequisiteResult
            {
                IsValid = false,
                ErrorMessage = errorMessage ?? string.Empty,
                MissingPrerequisites = missingPrerequisites ?? Array.Empty<string>(),
                MissingBuildings = missingBuildings ?? Array.Empty<string>()
            };
    }
}