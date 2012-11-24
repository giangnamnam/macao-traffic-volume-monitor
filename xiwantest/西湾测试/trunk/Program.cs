using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor.Testing
{
    class Program
    {
        static Lane lane;
        private static DiskCaptureRetriever captureRetriever;

        static void Main(string[] args)
        {
            captureRetriever = new DiskCaptureRetriever(@"..\..\西湾测试\测试\测试图片\{0}.jpg");

            lane = new Lane(@"..\..\西湾算法\mask-Lane1.gif");

            AnalyzeTestPlan testPlan;
            using (XmlReader reader = XmlReader.Create((@"..\..\西湾测试\测试\test plan.xml")))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AnalyzeTestPlan));
                testPlan = (AnalyzeTestPlan)serializer.Deserialize(reader);
            }
            AnalyseTest(testPlan);

            //BuildTestPlan();
        }

        static void AnalyseTest(AnalyzeTestPlan testPlan)
        {
            AnalyzeTestExpected expected;
            using (XmlReader reader = XmlReader.Create(testPlan.ExpectedDataPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AnalyzeTestExpected));
                expected = (AnalyzeTestExpected)serializer.Deserialize(reader);
            }


            AnalyzeTestAcutal actual = new AnalyzeTestAcutal();
            actual.AnalyzeOutputs = new AnalyzeOutput[testPlan.SampleLength];

            Console.WriteLine("将要开始测试。您可以输入此次测试的描述，或按回车跳过：");
            string comment = Console.ReadLine();
            if (string.IsNullOrEmpty(comment) == false)
                actual.Comment = comment;


            Queue<Image<Bgr, byte>> bufferImages = new Queue<Image<Bgr, byte>>(6);
            for (int i = 0; i < 6; i++)
            {
                bufferImages.Enqueue(captureRetriever.GetCapture());
            }


            //测试部分开始
            string formatString = "{0}{1}{2}";
            Console.WriteLine(formatString, padRightEx("序号", 10), padRightEx("人脑计数", 10), padRightEx("电脑计数", 10));
            for (int i = 0; i < actual.AnalyzeOutputs.Length; i++)
            {
                Image<Bgr, byte> orginialImage = bufferImages.ElementAt(3);
                ICollection<Image<Bgr, byte>> samples = bufferImages.ToArray();
                var laneCapture = lane.Analyze(orginialImage, samples);
                actual.AnalyzeOutputs[i] = new AnalyzeOutput { Id = i, CarNumber = laneCapture.Cars.Length };
                Console.WriteLine("{0,-10}{1,-10}{2,-10}", i, expected.AnalyzeOutputs[i].CarNumber, actual.AnalyzeOutputs[i].CarNumber);

                bufferImages.Dequeue();
                bufferImages.Enqueue(captureRetriever.GetCapture());
            }
            actual.ActualSum = actual.AnalyzeOutputs.Sum(o => o.CarNumber);
            actual.ExpectedSum = expected.AnalyzeOutputs.Sum(o => o.CarNumber);
            Console.WriteLine("{0}{1,-10}{2,-10}", padRightEx("和", 10), actual.ExpectedSum, actual.ActualSum);
            Console.WriteLine(Environment.NewLine + "测试完成");
            //测试部分结束

            Directory.CreateDirectory(testPlan.SampleFilePathPattern);
            string filePath = Path.Combine(testPlan.SampleFilePathPattern,
                        DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + " AnalyseTest.xml");
            using (XmlTextWriter xmlWriter = new XmlTextWriter(filePath, Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.Indented;
                XmlSerializer serializer = new XmlSerializer(typeof(AnalyzeTestAcutal));
                serializer.Serialize(xmlWriter, actual);
            }
        }

        static void BuildTestPlan()
        {
            AnalyzeTestExpected expected = new AnalyzeTestExpected();
            expected.AnalyzeOutputs = new[]
                                        {
                                           new AnalyzeOutput{ Id=0, CarNumber=7},
                                           new AnalyzeOutput{ Id=1, CarNumber=4},
                                           new AnalyzeOutput{ Id=2, CarNumber=3},
                                           new AnalyzeOutput{ Id=3, CarNumber=4},
                                           new AnalyzeOutput{ Id=4, CarNumber=9},
                                           new AnalyzeOutput{ Id=5, CarNumber=7},
                                           new AnalyzeOutput{ Id=6, CarNumber=6},
                                           new AnalyzeOutput{ Id=7, CarNumber=8},
                                           new AnalyzeOutput{ Id=8, CarNumber=7},
                                           new AnalyzeOutput{ Id=9, CarNumber=8},
                                           new AnalyzeOutput{ Id=10, CarNumber=15},
                                           new AnalyzeOutput{ Id=11, CarNumber=14},
                                           new AnalyzeOutput{ Id=12, CarNumber=16},
                                           new AnalyzeOutput{ Id=13, CarNumber=12},
                                           new AnalyzeOutput{ Id=14, CarNumber=6},
                                           new AnalyzeOutput{ Id=15, CarNumber=4},
                                           new AnalyzeOutput{ Id=16, CarNumber=4},
                                           new AnalyzeOutput{ Id=17, CarNumber=3},
                                           new AnalyzeOutput{ Id=18, CarNumber=4},
                                           new AnalyzeOutput{ Id=19, CarNumber=7},
                                           new AnalyzeOutput{ Id=20, CarNumber=8},
                                           new AnalyzeOutput{ Id=21, CarNumber=8}
                                        };

            XmlSerializer serializer = new XmlSerializer(typeof(AnalyzeTestExpected));

            XmlTextWriter xmlWriter = new XmlTextWriter("B:\\a.xml", Encoding.UTF8);
            xmlWriter.Formatting = Formatting.Indented;
            //serializer.Serialize(xmlWriter, expected);

            AnalyzeTestPlan testPlan = new AnalyzeTestPlan();
            testPlan.SampleLength = 21;
            testPlan.SampleFilePathPattern = @"D:\文件\毕业设计\西湾大桥氹仔端\图片";
            testPlan.ExpectedDataPath = @"D:\文件\毕业设计\西湾大桥氹仔端\测试\expected.xml";
            serializer = new XmlSerializer(typeof(AnalyzeTestPlan));
            xmlWriter = new XmlTextWriter("B:\\test plan.xml", Encoding.UTF8);
            xmlWriter.Formatting = Formatting.Indented;
            serializer.Serialize(xmlWriter, testPlan);

            //AnalyzeTestPlan testPlan = new AnalyzeTestPlan();
            //testPlan.
        }

        private static string padRightEx(string str, int totalByteCount)
        {
            Encoding coding = Encoding.GetEncoding("gb2312");
            int dcount = 0;
            foreach (char ch in str.ToCharArray())
            {
                if (coding.GetByteCount(ch.ToString()) == 2)
                    dcount++;
            }
            string w = str.PadRight(totalByteCount - dcount);
            return w;
        }
    }
}
