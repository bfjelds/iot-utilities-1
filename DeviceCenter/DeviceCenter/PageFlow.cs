using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.ComponentModel;

namespace DeviceCenter
{
    public class PageChangeCancelEventArgs : CancelEventArgs
    {
        public PageChangeCancelEventArgs(Page currentPage)
        {
            this.CurrentPage = currentPage;
        }

        public Page CurrentPage { get; private set; }
        public bool Close { get; set; }
    }

    public class PageFlow
    {
        private Dictionary<string, Page> _appPages = new Dictionary<string, Page>();
        private Frame _navigationFrame;
        private Page _currentPage;

        public PageFlow(Frame navigationFrame)
        {
            this._navigationFrame = navigationFrame;
            this._navigationFrame.Navigating += _navigationFrame_Navigating;
            this._navigationFrame.Navigated += _navigationFrame_Navigated;
        }

        private void _navigationFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            _currentPage = e.Content as Page;
        }

        public delegate void PageFlowCancelEventHandler(object sender, PageChangeCancelEventArgs e);

        public event PageFlowCancelEventHandler PageChange;

        private void _navigationFrame_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (PageChange != null)
            {
                PageChangeCancelEventArgs args = new PageChangeCancelEventArgs(_currentPage);
                PageChange(this, args);

                if (args.Cancel)
                    e.Cancel = true;
                else
                {
                    if (args.Close && _currentPage != null)
                    {
                        foreach (var item in _appPages.Where(kvp => kvp.Value == _currentPage).ToList())
                        {
                            _appPages.Remove(item.Key);
                        }
                    }
                }
            }
        }

        public void GoBack()
        {
            this._navigationFrame.GoBack();
        }

        public void Close(Page caller)
        {
            GoBack();

            foreach (var item in _appPages.Where(kvp => kvp.Value == caller).ToList())
            {
                _appPages.Remove(item.Key);
            }
        }

        public void Navigate(Type pageType, params object[] arguments)
        {
            Page page;
            string pageName = $"{pageType}:{string.Join(":", arguments)}";
            var fullParameters = (new object[] { this }).Concat(arguments).ToArray();

            if (!_appPages.TryGetValue(pageName, out page))
            {
                page = Activator.CreateInstance(pageType, fullParameters) as Page;
                System.Diagnostics.Debug.Assert(page != null);

                _appPages.Add(pageName, page);
            }

            this._navigationFrame.Navigate(page);
        }
    }
}
