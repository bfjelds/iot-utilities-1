using System.Collections.Generic;
using DeviceCenter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeviceCenterTests
{
    [TestClass()]
    public class AppDataTests
    {
        private UserInfo _userInfo1, _userInfo2, _userInfo3;
        public Dictionary<string, UserInfo> UserInfoList;
            
        [TestMethod()]
        public void StoreAndLoadWebBUserInfoTest()
        {
            InitializeTestData();

            var appData = new AppData();
            
            // store it
            appData.StoreWebBUserInfo(UserInfoList);

            //read it back
            var x = appData.LoadWebBUserInfo();

            // compare with original
            Assert.AreEqual(x[_userInfo1.DeviceName].UserName, _userInfo1.UserName);
            Assert.AreEqual(x[_userInfo1.DeviceName].Password, _userInfo1.Password);
            Assert.AreEqual(x[_userInfo2.DeviceName].UserName, _userInfo2.UserName);
            Assert.AreEqual(x[_userInfo2.DeviceName].Password, _userInfo2.Password);
            Assert.AreEqual(x[_userInfo3.DeviceName].UserName, _userInfo3.UserName);
            Assert.AreEqual(x[_userInfo3.DeviceName].Password, _userInfo3.Password);
        }

        private void InitializeTestData()
        {
            UserInfoList = new Dictionary<string, UserInfo>();

            _userInfo1 = new UserInfo
            {
                DeviceName = "UnitTestDeviceName1",
                UserName = "User1",
                Password = "1234567890"
            };

            _userInfo2 = new UserInfo
            {
                DeviceName = "UnitTestDeviceName2",
                UserName = "User2",
                Password = "abcdefghij"
            };

            _userInfo3 = new UserInfo
            {
                DeviceName = "UnitTestDeviceName3",
                UserName = "UserName3",
                Password = "Password3"
            };

            UserInfoList.Add(_userInfo1.DeviceName, _userInfo1);
            UserInfoList.Add(_userInfo2.DeviceName, _userInfo2);
            UserInfoList.Add(_userInfo3.DeviceName, _userInfo3);
        }
    }
}