using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimitedChromeManager
{
    public class SimpleWrapper<T>
    {
        public T Data;

        public SimpleWrapper(T data)
        {
            Data = data;
        }

        public static implicit operator T(SimpleWrapper<T> obj)
        {
            return obj.Data;
        }

        public static implicit operator SimpleWrapper<T>(T obj)
        {
            return new SimpleWrapper<T>(obj);
        }
    }
}
