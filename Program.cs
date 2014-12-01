using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SimpleActor
{
    class CountActor : Actor<int>
    {
        public long Sum { get; set; }
        public CountActor()
        {
            Sum = 0;
        }
        public override void DoWork(int item)
        {
            if (item == -1)
            {
                this.Exit();
                Console.WriteLine("********************************" + MessageCount + "," + Sum);
            }
            Sum += item;
            Console.WriteLine("item={0},summary={1}", item, Sum);
        }
    }

    class NoConcurrentExceptionActor : Actor<int>
    {
        private readonly IList<int> _list = new List<int>();

        public override void DoWork(int item)
        {
            bool exist = false;
            foreach (var i in _list)
            {
                if (i == item)
                {
                    Console.WriteLine("got a match, " + item);
                    exist = true;
                    break;
                }
            }
            if (!exist)
            {
                _list.Add(item);
            }
        }

        public void DoBadWork(int item)
        {
            bool exist = false;
            foreach (var i in _list)
            {
                if (i == item)
                {
                    Console.WriteLine("got a match, " + item);
                    exist = true;
                    break;
                }
            }
            if (!exist)
            {
                _list.Add(item);
            }
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            /* test for CountActor
        	var ca = new CountActor();
            for (int i = 0; i < 1000; i++)
            {
                ca.AddWorkItem(i);
            }
            ca.AddWorkItem(-1);
            */

            var ncea = new NoConcurrentExceptionActor();
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    ncea.AddWorkItem(i);
                }
            });
            Task.Factory.StartNew(() =>
            {
                for (int i = 567; i < 12345; i++)
                {
                    ncea.AddWorkItem(i);
                }
            });
            Task.Factory.StartNew(() =>
            {
                for (int i = 20000; i < 30000; i++)
                {
                    ncea.AddWorkItem(i);
                }
            });

//            Task.Factory.StartNew(() =>
//            {
//                for (int i = 0; i < 10000; i++)
//                {
//                    ncea.DoBadWork(i);
//                }
//            });
//            Task.Factory.StartNew(() =>
//            {
//                for (int i = 567; i < 12345; i++)
//                {
//                    ncea.DoBadWork(i);
//                }
//            });
//            Task.Factory.StartNew(() =>
//            {
//                for (int i = 20000; i < 30000; i++)
//                {
//                    ncea.DoBadWork(i);
//                }
//            });
            Console.ReadLine();
        }


    }
}
