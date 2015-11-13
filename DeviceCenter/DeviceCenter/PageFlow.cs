using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;

namespace DeviceCenter
{
    public class PageFlow
    {
        private Dictionary<string, Page> _appPages = new Dictionary<string, Page>();
        private Frame _navigationFrame;

        public PageFlow(Frame navigationFrame)
        {
            this._navigationFrame = navigationFrame;
        }

        public void GoBack()
        {
            this._navigationFrame.GoBack();
        }

        public void Navigate(Type pageType, params object[] arguments)
        {
            Page page;
            string pageName = $"{pageType}:{string.Join(":", arguments)}";
            var fullParameters = (new object[] { this }).Concat(arguments).ToArray();

            if (!_appPages.TryGetValue(pageName, out page))
            {
                fullParameters[0] = this;

                page = Activator.CreateInstance(pageType, fullParameters) as Page;
                System.Diagnostics.Debug.Assert(page != null);

                _appPages.Add(pageName, page);
            }

            this._navigationFrame.Navigate(page);
        }
    }
}
