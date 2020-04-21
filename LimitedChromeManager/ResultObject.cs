using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimitedChromeManager
{
    public class ResultObject<TObj,TEnum> where TEnum : System.Enum
    {
        public bool IsSuccess;
        public TEnum StatusCode;
        public TObj Result;
        public string description;
        public Exception Error;

        public static ResultObject<TObj, TEnum> Fail(TEnum statusCode, string desc, TObj resultObj = default, Exception error = null)
        {
            ResultObject<TObj, TEnum> result = new ResultObject<TObj, TEnum>()
            {
                IsSuccess = false,
                StatusCode = statusCode,
                description = desc,
                Error = error
            };
            return result;
        }

        public static ResultObject<TObj, TEnum> Success(TEnum statusCode, string desc, TObj resultObj = default, Exception error = null)
        {
            ResultObject<TObj, TEnum> result = new ResultObject<TObj, TEnum>()
            {
                IsSuccess = true,
                StatusCode = statusCode,
                description = desc,
                Result = resultObj
            };
            return result;
        }

        public static implicit operator bool(ResultObject<TObj, TEnum> resultObj)
        {
            return resultObj.IsSuccess;
        }
    }



}
