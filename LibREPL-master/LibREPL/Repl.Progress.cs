using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Piksel.LibREPL
{
    partial class Repl
    {
        public void StartSpinner()
        {
            _spinnerPos = Console.CursorLeft;
            _progressWorker = new BackgroundWorker();
            _progressWorker.DoWork += _spinnerThread_DoWork;
            _progressWorker.RunWorkerCompleted += _spinnerThread_RunWorkerCompleted;
            _progressWorker.WorkerSupportsCancellation = true;
            _progressWorker.RunWorkerAsync();
        }

        private void _spinnerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var ct = Console.CursorTop;
            var cl = Console.CursorLeft;
            Console.SetCursorPosition(_spinnerPos, _spinnerTop);
            Console.Write(_spinnerEnd);
            Console.SetCursorPosition(cl, ct);
        }

        public void StopSpinner(string end = " ")
        {
            _spinnerTop = Console.CursorTop;
            _spinnerEnd = end;
            _progressWorker.CancelAsync();
            Console.WriteLine();
        }

        private void _spinnerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            var self = (BackgroundWorker)sender;

            var si = 0;
            var sca = new char[] { '|', '/', '-', '\\' };
            do
            {
                if (self.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                Console.CursorLeft = _spinnerPos;
                Console.Write(sca[si]);
                if (++si > 3) si = 0;
                Thread.Sleep(100);
            } while (true);
        }
    }
}
