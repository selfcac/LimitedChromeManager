using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LimitedChromeManager
{
    public class ThreadTask<T>
    {
        T result = default(T);
        Exception threadException = null;
        bool threadFinishedSuccessfully = false;

        public delegate T threadAction();

        delegate void threadDelegate();
        threadDelegate myAction = null;
        Thread myThread = null;

        public ThreadTask(threadAction mainAction) {
            myAction = ()=> {
                try
                {
                    result = (T)(mainAction?.DynamicInvoke() ?? default(T));
                    threadFinishedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            };
        }

        public void Start()
        {
            myThread = new Thread(new ThreadStart(myAction));
            myThread.Start();
        }

        public static void fastThread(
            threadAction mainAction, Action<T> onSucess,
            Action<Exception> onThreadError,
            Action<Exception> onError)
        {
            try
            {
                ThreadTask<T> tt = new ThreadTask<T>(mainAction);
                tt.Start();
                if (tt.Join())
                {
                    onSucess(tt.Result());
                }
                else
                {
                    onThreadError(tt.GetError());
                }
            }
            catch (Exception ex)
            {
                onError(ex);
            }
        }

        public bool Join()
        {
            myThread?.Join();
            return threadFinishedSuccessfully;
        }

        public T Result()
        {
            return result;
        }

        public Exception GetError()
        {
            return threadException;
        }
    }
}
