using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Speech.Synthesis;
using System.Web;

namespace webbro
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public SpeechSynthesizer debugger;
        public double performanceTime = 0.0;

        public MainWindow()
        {
            InitializeComponent();

            Initialize();

        }

        public void Initialize()
        {
            debugger = new SpeechSynthesizer();
        }

        private void SendRequestHandler(object sender, RoutedEventArgs e)
        {
            SendRequest();
        }

        private void SendRequest()
        {

            SetInterval(() =>
            {
                performanceTime += 0.1;
            }, TimeSpan.FromSeconds(0.1));
            string webhookBoxContent = webHookBox.Text;
            int methodIndex = webHookMethodBox.SelectedIndex;
            ComboBoxItem selectedWebHookMethodBoxItem = ((ComboBoxItem)(webHookMethodBox.Items[methodIndex]));
            string method = ((string)(selectedWebHookMethodBoxItem.Content));
            // https://jsonplaceholder.typicode.com/posts/1
            string uriPath = webhookBoxContent;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(uriPath);
                webRequest.Method = method;
                webRequest.UserAgent = ".NET Framework Test Client";
                string authorizationHeaderBoxContent = authorizationHeaderBox.Text;
                webRequest.Headers["Authorization"] = authorizationHeaderBoxContent;
                try
                {
                    if (method != "GET")
                    {
                        StringBuilder postData = new StringBuilder();
                        foreach (StackPanel queryParam in queryParams.Children)
                        {
                            TextBox queryParamKey = ((TextBox)(queryParam.Children[0]));
                            TextBox queryParamValue = ((TextBox)(queryParam.Children[1]));
                            string queryParamKeyContent = queryParamKey.Text;
                            string queryParamValueContent = queryParamValue.Text;
                            postData.Append(HttpUtility.UrlEncode(String.Format(queryParamKeyContent + "={0}&", queryParamValueContent)));
                        }
                        Stream postStream = webRequest.GetRequestStream();
                        postStream.Write(new byte[] { }, 0, 0);
                    }
                    using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                    {
                        using (var reader = new StreamReader(webResponse.GetResponseStream()))
                        {
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            var objText = reader.ReadToEnd();
                            responseBox.Text = objText;
                            CookieCollection responseCookies = webResponse.Cookies;
                            int countResponseCookies = responseCookies.Count;
                            string rawCountResponseCookies = countResponseCookies.ToString();
                            responseCookiesLabel.Header = "Куки (" + rawCountResponseCookies + ")";
                            bool isCookiesExists = countResponseCookies >= 1;
                            if (isCookiesExists)
                            {
                                responseCookiesBox.Text = responseCookies.ToString();
                            }
                            WebHeaderCollection responseHeaders = webResponse.Headers;
                            int countResponseHeaders = responseHeaders.Count;
                            string rawCountResponseHeaders = countResponseHeaders.ToString();
                            responseHeadersLabel.Header = "Заголовки (" + rawCountResponseHeaders + ")";
                            bool isHeadersExists = countResponseHeaders >= 1;
                            if (isHeadersExists)
                            {
                                responseHeadersBox.Text = responseHeaders.ToString();
                            }
                            HttpStatusCode statusCode = webResponse.StatusCode;
                            int numberStatusCode = ((int)(statusCode));
                            string rawStatusCode = statusCode.ToString();
                            string statusCodeLabelContent = numberStatusCode + " " + rawStatusCode;
                            statusCodeLabel.Text = statusCodeLabelContent;
                            long contentLength = webResponse.ContentLength;
                            string measure = "Б";
                            string rawContentLength = contentLength.ToString();
                            if (contentLength / (1024 * 1024) >= 1)
                            {
                                measure = "Мб";
                                rawContentLength = (contentLength / (1024 * 1024)).ToString();
                            }
                            else if (contentLength / (1024) >= 1)
                            {
                                measure = "Кб";
                                rawContentLength = (contentLength / 1024).ToString();
                            }
                            string contentLengthLabelContent = rawContentLength + " " + measure;
                            contentLengthLabel.Text = contentLengthLabelContent;
                            string rawPerformanceTime = performanceTime.ToString("0.0");
                            string performanceTimeLabelContent = rawPerformanceTime + " мс";
                            performanceTimeLabel.Text = performanceTimeLabelContent;
                        }
                    }
                }
                catch (WebException)
                {
                    debugger.Speak("Ошибка запроса");
                    responseBox.Text = "Ошибка запроса";
                }
            }
            catch (UriFormatException)
            {
                responseBox.Text = "Ошибка запроса";
            }
            performanceTime = 0;
        }

        private void UpdateQueryParamsHandler(object sender, KeyEventArgs e)
        {
            TextBox field = ((TextBox)(sender));
            string fieldContent = field.Text;
            int fieldContentLength = fieldContent.Length;
            StackPanel queryParam = ((StackPanel)(field.Parent));
            int queryParamIndex = queryParams.Children.IndexOf(queryParam);
            Key currentKey = e.Key;
            Key enterKey = Key.Enter;
            Key backSpaceKey = Key.Back;
            bool isEnterKey = currentKey == enterKey;
            bool isBackSpaceKey = currentKey == backSpaceKey;
            bool isContentEmpty = fieldContentLength <= 0;
            bool isRemoveQueryParam = isBackSpaceKey && isContentEmpty;
            bool isUpdateQueryParams = isEnterKey || isRemoveQueryParam;
            if (isUpdateQueryParams)
            {
                UpdateQueryParams(isEnterKey, queryParamIndex);
            }
        }

        public void UpdateQueryParams(bool isAddQueryParam, int index)
        {
            if (isAddQueryParam)
            {
                StackPanel queryParam = new StackPanel();
                queryParam.Orientation = Orientation.Horizontal;
                TextBox queryParamKey = new TextBox();
                queryParamKey.Margin = new Thickness(35, 5, 35, 5);
                queryParamKey.Width = 75;
                queryParam.Children.Add(queryParamKey);
                TextBox queryParamValue = new TextBox();
                queryParamValue.Margin = new Thickness(35, 5, 35, 5);
                queryParamValue.Width = 75;
                queryParam.Children.Add(queryParamValue);
                queryParams.Children.Add(queryParam);
                queryParamKey.KeyUp += UpdateQueryParamsHandler;
                queryParamValue.KeyUp += UpdateQueryParamsHandler;
                queryParamKey.Focus();
            }
            else
            {
                int queryParamsCount = queryParams.Children.Count;
                int lastQueryParamIndex = queryParamsCount - 1;
                bool isCanRemove = queryParamsCount >= 2;
                if (isCanRemove)
                {
                    // queryParams.Children.RemoveAt(lastQueryParamIndex);
                    queryParams.Children.RemoveAt(index);
                }
            }
        }

        public async Task SetInterval(Action action, TimeSpan timeout)
        {
            await Task.Delay(timeout).ConfigureAwait(false);
            action();
            SetInterval(action, timeout);
        }

    }
}
