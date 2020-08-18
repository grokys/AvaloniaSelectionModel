using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Threading
{
    public class Dispatcher
    {
        public static Dispatcher UIThread { get; } = new Dispatcher();

        public bool CheckAccess() => true;
        public void VerifyAccess()
        {
        }

        public void Post(Action a)
        {
        }
    }
}
