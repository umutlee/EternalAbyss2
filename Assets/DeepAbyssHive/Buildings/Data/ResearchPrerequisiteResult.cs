#if false
// Disabled to avoid duplicate type with existing project definition.

using System;
using System.Collections.Generic;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 研究前置條件檢查結果
    /// </summary>
    [Serializable]
    public struct ResearchPrerequisiteResult
    {
        /// <summary>
        /// 是否滿足前置條件
        /// </summary>
        public bool IsValid;
        
        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage;
        
        /// <summary>
        /// 缺少的前置研究項目
        /// </summary>
        public List<string> MissingPrerequisites;
        
        /// <summary>
        /// 缺少的建築類型
        /// </summary>
        public List<BuildingType> MissingBuildings;

        /// <summary>
        /// 創建有效的檢查結果
        /// </summary>
        /// <returns>有效的檢查結果</returns>
        public static ResearchPrerequisiteResult Valid()
            => new ResearchPrerequisiteResult { IsValid = true };

        /// <summary>
        /// 創建無效的檢查結果
        /// </summary>
        /// <param name="error">錯誤訊息</param>
        /// <param name="missingPrereq">缺少的前置條件</param>
        /// <param name="missingBuildings">缺少的建築</param>
        /// <returns>無效的檢查結果</returns>
        public static ResearchPrerequisiteResult Invalid(
            string error,
            List<string> missingPrereq = null,
            List<BuildingType> missingBuildings = null)
            => new ResearchPrerequisiteResult
            {
                IsValid = false,
                ErrorMessage = error,
                MissingPrerequisites = missingPrereq ?? new List<string>(),
                MissingBuildings = missingBuildings ?? new List<BuildingType>()
            };
    }
}

#endif