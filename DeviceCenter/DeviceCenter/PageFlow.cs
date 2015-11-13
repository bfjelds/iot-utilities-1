using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace DeviceCenter
{
    public class PageFlow
    {
        public PageFlow(Frame navigationFrame)
        {
            this._navigationFrame = navigationFrame;
        }

        public void GoBack()
        {
            this._navigationFrame.GoBack();
        }

        private Dictionary<string, Page> _mainPages = new Dictionary<string, Page>();

        public void Navigate(Type pageType, params object[] arguments)
        {
            Page page;
            string pageName = pageType.ToString();

            object[] fullArguments = new object[arguments.Length + 1];
            fullArguments[0] = this;

            int i = 1;
            foreach (var param in arguments)
            {
                fullArguments[i++] = param;
                pageName += (":" + param.ToString());
            }

            if (!_mainPages.TryGetValue(pageName, out page))
            {
                fullArguments[0] = this;

                page = Activator.CreateInstance(pageType, fullArguments) as Page;
                System.Diagnostics.Debug.Assert(page != null);

                _mainPages.Add(pageName, page);
            }

            this._navigationFrame.Navigate(page);
        }

        private Frame _navigationFrame;
    }
}
