﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using AlfaBank.AFT.Core.Helpers;
using AlfaBank.AFT.Core.Infrastructures.Web;
using AlfaBank.AFT.Core.Model.Context;
using AlfaBank.AFT.Core.Models.Web;
using AlfaBank.AFT.Core.Models.Web.Attributes;
using AlfaBank.AFT.Core.Models.Web.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using Selenium.WebDriver.WaitExtensions;

namespace AlfaBank.AFT.Core.Models.Context
{
    public class WebContext
    {
        private readonly Dictionary<string, Type> _allPages = null;
        private IPage _currentPage = null;

        private readonly Driver _driver;
        private readonly VariableContext _context;

        public WebContext(Driver driver, VariableContext context)
        {
            this._driver = driver;
            this._context = context;
            _allPages = InitializePages();
        }
        public void Start(BrowserType browser, bool remote = false, DriverOptions options = null, string version = null, string url = null, PlatformType platform = PlatformType.Any)
        {
            if (!(_driver.WebDriver is null))
            {
                return;
            }

            if (remote)
            {
                if ((version is null) || (url is null))
                {
                    return;
                }

                switch (browser)
                {
                    case BrowserType.Chrome:
                    case BrowserType.Mozila:
                    {
#pragma warning disable 618
                        var capabilities = new DesiredCapabilities(browser.ToString().ToLower(), version, new Platform(platform));
                        capabilities?.SetCapability("enableVNC", true);
#pragma warning restore 618

                        _driver.WebDriver = new RemoteWebDriver(new Uri(url), capabilities);
                        return;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(browser), browser, null);
                }
            }

            switch (browser)
            {
                case BrowserType.Chrome:
                case BrowserType.Mozila:
                    Start(browser, options);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(browser), browser, null);
            }
        }
        public void Start(BrowserType browser, DriverOptions options = null)
        {
            switch (browser)
            {
                case BrowserType.Chrome:
                {
                    _driver.WebDriver = options != null ? new ChromeDriver((ChromeOptions)options) : new ChromeDriver();
                    break;
                }
                case BrowserType.Mozila:
                {
                    _driver.WebDriver = options != null ? new FirefoxDriver((FirefoxOptions)options) : new FirefoxDriver();
                    break;
                }
                default:
                    throw new ArgumentNullException(
                        $"Неизвестный тип драйвера \"{browser}\". Невозможно проинициализировать драйвер.");
            }
        }
        public void Stop()
        {
            if (_driver.WebDriver is null)
            {
                return;
            }

            try
            {
                _driver.WebDriver.Quit();
                _driver.WebDriver.Dispose();
            }
            catch (Exception)
            {
                throw new SystemException("Остановить WebDriver не удалось.");
            }

            _driver.WebDriver = null;
        }
        public void Dispose()
        {
            DisposeDriverService.FinishHim(_driver.WebDriver);
        }
        public void SetCurrentPageBy(string name, bool withLoad = false)
        {
            if (_allPages.Any())
            {
                if (_allPages.ContainsKey(name))
                {
                    _currentPage = (IPage)Activator.CreateInstance(_allPages[name], _driver, _context);

                    if(withLoad)
                    {
                        _currentPage.GoToPage();
                    }

                    _currentPage.IsPageLoad();
                }
                else
                {
                    throw new NullReferenceException($"Не найдена страница с названием \"{name}\". Убедитесь в наличии атрибута [Page] у классов страниц.");
                }
            }
            else
            {
                throw new NullReferenceException($"Не найдены страницы. Убедитесь в наличии атрибута [Page] у классов страниц и подключений их к проекту с тестами.");
            }
        }
        public IPage GetCurrentPage()
        {
            if (_currentPage == null) throw new NullReferenceException("Текущая страница не задана");
            return _currentPage;
        }
        public void SetTimeout(int sec)
        {
            this._driver.Timeout = sec;
        }
        public int GetTimeout() => _driver.Timeout;
        public void SetSizeBrowser(int width, int height)
        {
            if (this._driver.WebDriver != null)
                this._driver.WebDriver.Manage().Window.Size = new Size(width, height);
        }
        public Size GetSizeBrowser()
        {
            if (this._driver.WebDriver != null)
                return this._driver.WebDriver.Manage().Window.Size;

            return new Size(0, 0);
        }
        public void GoToUrl(string url)
        {
            this._driver.WebDriver.Navigate().GoToUrl(new Uri(url));
            this._driver.WebDriver.Wait(this._driver.Timeout).ForPage().ReadyStateComplete();
        }
        public void Maximize()
        {
            _driver.WebDriver?.Manage().Window.Maximize();
        }
        public void GoToTab(int number)
        {
            try
            {
                _driver.WebDriver.SwitchTo().Window(_driver.WebDriver.WindowHandles[number]);
            }
            catch (NoSuchWindowException)
            {
                throw new NoSuchWindowException($"Вкладки с номером \"{number}\" не найдено.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException($"Вкладки с номером \"{number}\" не найдено.");
            }
        }
        public int GetCountTabs() => this._driver.WebDriver.WindowHandles.Count();
        private Dictionary<string, Type> InitializePages()
        {
            var projects = AppDomain.CurrentDomain.GetAssemblies();
            Dictionary<string, Type> allClasses = new Dictionary<string, Type>();

            foreach (var project in projects)
            {
                var classes = project.GetTypes().Where(t => t.IsClass).Where(t => t.GetCustomAttribute(typeof(PageAttribute), true) != null);

                foreach (var cl in classes)
                {
                    allClasses.Add(cl.GetCustomAttribute<PageAttribute>().Name, cl);
                }

            }

            return allClasses;
        }
    }
}