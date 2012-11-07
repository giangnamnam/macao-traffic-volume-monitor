using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Gqqnbig.TrafficVolumeCalculator.Testing
{
    class Program
    {
        static Lane lane;
        private const string resultPath = @"D:\文件\毕业设计\西湾大桥氹仔端\测试\TestResults";

        static void Main(string[] args)
        {
            DiskCaptureRetriever captureRetriever = new DiskCaptureRetriever(@"D:\文件\毕业设计\西湾大桥氹仔端\图片\{0}.jpg");

            lane = new Lane(captureRetriever);
            //BuildTestBase();

            AnalyseTest();
        }

        static void AnalyseTest()
        {
            int[] expected = new int[22];

            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreWhitespace = true;
            using (XmlReader xmlReader = XmlReader.Create(@"D:\文件\毕业设计\西湾大桥氹仔端\测试\test.xml", readerSettings))
            {
                xmlReader.Read();//<?xml version="1.0" encoding="utf-8"?>
                xmlReader.Read();//<root>
                for (int i = 0; i < expected.Length; i++)
                {
                    xmlReader.Read();
                    xmlReader.Read();
                    expected[i] = xmlReader.ReadContentAsInt();
                }
            }


            //测试部分开始
            int[] actual = new int[expected.Length];
            string formatString = "{0}{1}{2}";
            Console.WriteLine(formatString, padRightEx("序号", 10), padRightEx("人脑计数", 10), padRightEx("电脑计数", 10));
            for (int i = 0; i < actual.Length; i++)
            {
                var laneCapture = lane.Analyze(i);
                actual[i] = laneCapture.Cars.Length;
                Console.WriteLine("{0,-10}{1,-10}{2,-10}", i, actual[i], expected[i]);
            }
            Console.WriteLine("{0}{1,-10}{2,-10}", padRightEx("和",10), actual.Sum(), expected.Sum());
            Console.WriteLine(Environment.NewLine + "测试完成");
            //测试部分结束


            string filePath = Path.Combine(resultPath,
                DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + " AnalyseTest.txt");
            Directory.CreateDirectory(resultPath);
            XmlTextWriter xmlWriter = new XmlTextWriter(filePath, System.Text.Encoding.UTF8);
            xmlWriter.Formatting = Formatting.Indented;

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("root");
            for (int i = 0; i < 22; i++)
            {
                xmlWriter.WriteStartElement("carNumber");
                xmlWriter.WriteAttributeString("id", i.ToString());
                xmlWriter.WriteString(actual[i].ToString());
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }




        private static void BuildTestBase()
        {
            Console.Write("请输入焦点图的存放位置：");
            string focusedImagePath = Console.ReadLine();
            Directory.CreateDirectory(focusedImagePath);

            Console.Write("请输入测试基准文件的地址：");
            string testBaseFilePath = Console.ReadLine();
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = " ";
            settings.NewLineChars = Environment.NewLine;
            settings.Encoding = System.Text.Encoding.UTF8;


            XmlTextWriter xmlWriter = new XmlTextWriter(testBaseFilePath, System.Text.Encoding.UTF8);
            xmlWriter.Formatting = Formatting.Indented;

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("root");
            XmlSerializer serializer = new XmlSerializer(typeof(int));
            for (int i = 0; i < 22; i++)
            {
                var laneCapture = lane.Analyze(i);
                xmlWriter.WriteStartElement("carNumber");
                xmlWriter.WriteAttributeString("id", i.ToString());
                xmlWriter.WriteString(laneCapture.Cars.Length.ToString());
                xmlWriter.WriteEndElement();

                //laneCapture.FocusedImage.Save(Path.Combine(focusedImagePath, i + ".jpg"));
            }
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
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
