using Microsoft.VisualStudio.TestTools.UnitTesting;
using DeviceCenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace DeviceCenter.Tests
{
    [TestClass()]
    public class PageDeviceConfigurationTests
    {
        [TestMethod()]
        public void ContainsInvalidCharsTest()
        {
            List<string> failTestCases = new List<string>()
            {
                "t`est",
                "t~est",
                "t!est",
                "t@est",
                "t#est",
                "t$est",
                "t%est",
                "t^est",
                "t&est",
                "t*est",
                "t(est",
                "t)est",
                "t+est",
                "t=est",
                "t[est",
                "t]est",
                "t{est",
                "t}est",
                "t\\est",
                "t/est",
                "t;est",
                "t:est",
                "t.est",
                "t'est",
                "t\"est",
                "t<est",
                "t>est",
                "t?est",
            };

            List<string> successTestCases = new List<string>()
            {
                "t",
                "test",
                "testing",
                "test123",
                "1",
                "123",
                "test1",
                "test_0",
            };

            foreach(string str in failTestCases)
            {
                if(Regex.IsMatch(str, PageDeviceConfiguration.InvalidCharsRegexPattern) != true)
                {
                    Assert.Fail("Failed test case: " + str);
                }
            }

            foreach(string str in successTestCases)
            {
                if(Regex.IsMatch(str, PageDeviceConfiguration.InvalidCharsRegexPattern) != false)
                {
                    Assert.Fail("Failed test case: " + str);
                }
            }
        }
    }
}