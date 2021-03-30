using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace log4net_logdna_console
{
    internal class GlobalContextTest : IFixingRequired
    {
        public object GetFixedObject()
        {
            return ToString();
        }

        public override string ToString()
        {
            return DateTime.UtcNow.Millisecond.ToString();
        }
    }

    internal class Program
    {
        private static void Main(string[] argArray)
        {
            GlobalContext.Properties["GlobalContextPropertySample"] = new GlobalContextTest();

            var currentFileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo(currentFileName + ".config"));

            var log = LogManager.GetLogger(typeof(Program));

            Thread thread = Thread.CurrentThread;
            thread.Name = "Main Thread";
            ThreadContext.Properties["MainThreadContext"] = "MainThreadContextValue";

            log.Info($"================ test start ================");
            log.Error("oops", new ArgumentOutOfRangeException("argArray"));
            log.Warn("hmmm", new ApplicationException("app exception"));
            log.Info("yawn");

            Thread newThread1 = new Thread(() =>
            {
                Thread curntThread = Thread.CurrentThread;
                curntThread.Name = "Inner thread 1";
                ThreadContext.Properties["InnerThread1Context"] = "InnerThreadContext1Values";
                LogicalThreadContext.Properties["InnerLogicalThreadContext"] = "InnerLogicalThreadContextValues";

                using (ThreadContext.Stacks["NDC1"].Push("StackValue1"))
                {
                    log.Info("this is an inner thread 1");

                    using (ThreadContext.Stacks["NDC1"].Push("StackValue2"))
                    {
                        log.Info("inner ndc of inner thread 1");
                    }
                }

                using (LogicalThreadContext.Stacks["LogicalThread1"].Push("LogicalThread1_Stack"))
                {
                    log.Info("logical thread context 1 stack");
                    using (LogicalThreadContext.Stacks["LogicalThread1"].Push("LogicalThread1_Stack_2"))
                    {
                        log.Info("logical thread context 2 stack");
                    }
                }

                log.Info("without ndc of inner thread 1");
            });

            newThread1.Start();

            Thread newThread2 = new Thread(() =>
            {
                Thread curntThread = Thread.CurrentThread;
                curntThread.Name = "Inner thread 2";
                ThreadContext.Properties["InnerThread2Context"] = "InnerThreadContext2Values";
                log.Info("this is an inner thread 2");
            });

            newThread2.Start();

            //Test self referencing
            var parent = new Person { Name = "John Smith" };
            var child1 = new Person { Name = "Bob Smith", Parent = parent };
            var child2 = new Person { Name = "Suzy Smith", Parent = parent };
            parent.Children = new List<Person> { child1, child2 };
            log.Info(parent);

            log.Debug(@"This
            is
            some
            multiline
            log");
            log.InfoFormat("Logdna is the best {0} to collect Logs.", "service");
            log.Info(new { type1 = "newcustomtype", value1 = "newcustomvalue" });
            log.Info(new TestObject());
            log.Info(null);

            try
            {
                try
                {
                    try
                    {
                        try
                        {
                            throw new Exception("1");
                        }
                        catch (Exception e)
                        {
                            throw new Exception("2", e);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("3", e);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("4", e);
                }
            }
            catch (Exception e)
            {
                log.Error("Exception", e);
            }

            log.Info("This is the last message. Program will terminate now.finished");

            //有值的輸出
            Dictionary<string, string> testDic = new Dictionary<string, string>();
            testDic.Add("require", "true");
            testDic.Add("baseUri", "https://abcd.com");
            log.Info(testDic);

            //空值的輸出
            Dictionary<string, string> emptyDic = new Dictionary<string, string>();
            log.Info(emptyDic);

            //數值的輸出
            Dictionary<int, string> intDic = new Dictionary<int, string>();
            intDic.Add(1, "testData");
            intDic.Add(2, "testBBB");
            log.Info(intDic);

            //類別的輸出
            Dictionary<string, Person> personDic = new Dictionary<string, Person>();
            personDic.Add("man1", new Person { Name = "Man" });
            personDic.Add("man2", new Person { Name = "ManMan" });
            log.Info(personDic);

            //清單的輸出
            List<string> tmpList = new List<string>();
            tmpList.Add("List1");
            tmpList.Add("List2");
            log.Warn(tmpList);

            //數值清單的輸出
            List<int> tmpIntList = new List<int>();
            Random intRnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < 3; i++)
            {
                tmpIntList.Add(intRnd.Next(1, 300000));
            }
            log.Debug(tmpIntList);

            //類別清單的輸出
            List<Person> personList = new List<Person>();
            personList.Add(new Person { Name = "first Person" });
            personList.Add(new Person { Name = "second Person" });
            log.Warn(personList);

            //Json output
            log.Debug("{\"$type\":\"System.Collections.Generic.List<Eds.MessageQueue.Task.ValueObjects.RunningTask>\",\"$values\":[{\"$type\":\"Eds.MessageQueue.Task.ValueObjects.RunningTask\",\"No\":0,\"TaskId\":2567,\"Stake\":0,\"Stage\":\"FT\",\"ScoreType\":\"GOALS\",\"MarketType\":\"OU\",\"BetCount\":0,\"TaskMappings\":{\"$type\":\"System.Collections.Generic.List<Eds.MessageQueue.Task.ValueObjects.TaskMapping>\",\"$values\":[{\"$type\":\"Eds.MessageQueue.Task.ValueObjects.TaskMapping\",\"Website\":\"SINGBET\",\"League\":\"Arena Cup (In Croatia)\",\"GameTime\":\"2021-01-13T06:00:00\",\"HomeTeam\":\"NK Medimurje\",\"AwayTeam\":\"HNK Sibenik\",\"Choice\":\"\"}]},\"TaskLines\":null,\"BetSummaryList\":{\"$type\":\"System.Collections.Generic.List<Eds.MessageQueue.Task.ValueObjects.TaskBetSummary>\",\"$values\":[{\"$type\":\"Eds.MessageQueue.Task.ValueObjects.TaskBetSummary\",\"BetType\":\"UNDER\",\"Line\":3.5,\"ConfirmedStake\":287,\"PendingStake\":0,\"AveragePrice\":0.7}]}},{\"$type\":\"Eds.MessageQueue.Task.ValueObjects.RunningTask\",\"No\":0,\"TaskId\":2569,\"Stake\":0,\"Stage\":\"FT\",\"ScoreType\":\"GOALS\",\"MarketType\":\"OU\",\"BetCount\":0,\"TaskMappings\":{\"$type\":\"System.Collections.Generic.List<Eds.MessageQueue.Task.ValueObjects.TaskMapping>\",\"$values\":[{\"$type\":\"Eds.MessageQueue.Task.ValueObjects.TaskMapping\",\"Website\":\"SINGBET\",\"League\":\"EFootball - Battle - 8 Mins Play\",\"GameTime\":\"2021-01-13T02:42:00\",\"HomeTeam\":\"Atalanta  Esports\",\"AwayTeam\":\"AC Milan  Esports\",\"Choice\":\"\"}]},\"TaskLines\":null,\"BetSummaryList\":{\"$type\":\"System.Collections.Generic.List<Eds.MessageQueue.Task.ValueObjects.TaskBetSummary>\",\"$values\":[]}},{\"$type\":\"Eds.MessageQueue.Task.ValueObjects.RunningTask\",\"No\":0,\"TaskId\":2568,\"Stake\":0,\"Stage\":\"FT\",\"ScoreType\":\"GOALS\",\"MarketType\":\"OU\",\"BetCount\":0,\"TaskMappings\":{\"$type\":\"System.Collections.Generic.List<Eds.MessageQueue.Task.ValueObjects.TaskMapping>\",\"$values\":[{\"$type\":\"Eds.MessageQueue.Task.ValueObjects.TaskMapping\",\"Website\":\"SINGBET\",\"League\":\"EFootball - FIFA 21 CLA Europa League - 10 Mins Play\",\"GameTime\":\"2021-01-13T02:28:00\",\"HomeTeam\":\"AZ Alkmaar  Esports\",\"AwayTeam\":\"Celtic  Esports\",\"Choice\":\"\"}]},\"TaskLines\":null,\"BetSummaryList\":{\"$type\":\"System.Collections.Generic.List<Eds.MessageQueue.Task.ValueObjects.TaskBetSummary>\",\"$values\":[]}}]}");

            log.Info("================ test end ================");

            System.Threading.Thread.Sleep(500);

            log.Logger.Repository.Shutdown();

            Console.WriteLine("done");
            Console.Read();
        }
    }
}