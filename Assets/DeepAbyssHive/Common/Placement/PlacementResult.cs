using System;
using UnityEngine;

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// 放置流程的標準錯誤碼
    /// </summary>
    public enum PlaceResultCode
    {
        OK = 0,
        E_PLACE_COLLISION = 1,
        E_OUT_OF_BOUNDS = 2,
        E_REQUIRE_CREEP = 3,
        E_INVALID_TYPE = 4
    }

    /// <summary>
    /// 統一回傳容器：所有放置/驗證 API 以此回傳
    /// </summary>
    public class Result<T>
    {
        public bool ok;
        public PlaceResultCode code;
        public T data;
        public string message;

        private Result(bool ok, PlaceResultCode code, T data, string message)
        {
            this.ok = ok;
            this.code = code;
            this.data = data;
            this.message = message;
        }

        public static Result<T> Success(T data, string message = null)
            => new Result<T>(true, PlaceResultCode.OK, data, message);

        public static Result<T> Fail(PlaceResultCode code, string message = null, T data = default)
            => new Result<T>(false, code, data, message);
    }

    /// <summary>
    /// 一些便利的工廠（常見型別）
    /// </summary>
    public static class PlacementResults
    {
        public static Result<Bounds> OkBounds(Bounds b) => Result<Bounds>.Success(b);
        public static Result<Bounds> Collision(string msg = null) => Result<Bounds>.Fail(PlaceResultCode.E_PLACE_COLLISION, msg);
        public static Result<Bounds> OutOfBounds(string msg = null) => Result<Bounds>.Fail(PlaceResultCode.E_OUT_OF_BOUNDS, msg);
        public static Result<Bounds> RequireCreep(string msg = null) => Result<Bounds>.Fail(PlaceResultCode.E_REQUIRE_CREEP, msg);
        public static Result<Bounds> InvalidType(string msg = null) => Result<Bounds>.Fail(PlaceResultCode.E_INVALID_TYPE, msg);
    }
}