using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EynyCrawler
{
    public class Crawler
    {
        public  string getHTML(string strUrl, Encoding encoding)
        {
            Uri url = new Uri(strUrl);
            WebClient wc = new WebClient();
            wc.Encoding = encoding;
            Stream s = wc.OpenRead(url);
            StreamReader sr = new StreamReader(s, encoding);
            return sr.ReadToEnd();
        }

        //取得網頁的html
        public string getHTMLbyWebRequest(string strUrl,string account ,string pwd)
        {
            string responseFromServer = String.Empty;
            try
            {
                //=====================
                //取得登入後的cookie
                string hostUri = System.Configuration.ConfigurationManager.AppSettings["hostUri"];
                hostUri = String.Format(hostUri + "member.php?mod=logging&action=login&loginsubmit=yes&handlekey=login&loginhash=LL3zz&inajax=1"
                   + "&loginfield=username&username={0}&password={1}&questionid=0&answer=&cookietime=2592000&loginsubmit=true",
                   account, pwd);
                var targetUri = new Uri(hostUri);

                System.Net.HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(hostUri);
                req.Method = "GET";
                req.CookieContainer = new CookieContainer();
                //增加同意瀏覽18禁網頁的cookie
                Cookie eynycookie = new Cookie("djAX_e8d7_agree", "576");
                eynycookie.Domain = "eyny.com";
                req.CookieContainer.Add(eynycookie);
                System.Net.HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                //======================


                Encoding encoding = System.Text.Encoding.Default;
                req = (HttpWebRequest)System.Net.WebRequest.Create(strUrl);
                req.Credentials = CredentialCache.DefaultCredentials;
                //把登入頁面的cookie加到這頁面
                req.CookieContainer = new CookieContainer();
                foreach (Cookie c in resp.Cookies)
                {
                    req.CookieContainer.Add(c);
                }
                //===========
                resp.Close();
                resp.Dispose();
                //增加同意瀏覽18禁網頁的cookie
                req.CookieContainer.Add(eynycookie);
                //再加一些莫名其妙的cookie
                Cookie authCookie = new Cookie("djAX_e8d7_auth", "333fUi0fk7ugLxnJFmn04gbTOlBKY10vvPpZixnDdrOCAXH2cwn6ngt%2FHEzmv0i%2ByLa28b2VZa20KDQCB3UcSlchFlVz");
                authCookie.Domain = "eyny.com";
                Cookie userCookie = new Cookie("username", account);
                userCookie.Domain = "eyny.com";
                Cookie activityCookie = new Cookie("djAX_e8d7_ulastactivity", "	74bb8U5QEAZKIT8ENCP3DhwtI3MpSl28JgoGoO2veMA4fZVVdTq4");
                activityCookie.Domain = "eyny.com";
                Cookie sidCookie = new Cookie("djAX_e8d7_sid", "IPc6OG");
                sidCookie.Domain = "eyny.com";

                req.CookieContainer.Add(authCookie);
                req.CookieContainer.Add(userCookie);
                req.CookieContainer.Add(activityCookie);
                req.CookieContainer.Add(sidCookie);

                HttpWebResponse response = (HttpWebResponse)req.GetResponse();

                if (response.StatusDescription.ToUpper() == "OK")
                {
                    switch (response.CharacterSet.ToLower())
                    {
                        case "gbk":
                            encoding = Encoding.GetEncoding("GBK");//貌似用GB2312就可以
                            break;
                        case "gb2312":
                            encoding = Encoding.GetEncoding("GB2312");
                            break;
                        case "utf-8":
                            encoding = Encoding.UTF8;
                            break;
                        case "big5":
                            encoding = Encoding.GetEncoding("Big5");
                            break;
                        case "iso-8859-1":
                            encoding = Encoding.UTF8;//ISO-8859-1的編碼用UTF-8處理，致少優酷的是這種方法沒有亂碼
                            break;
                        default:
                            encoding = Encoding.UTF8;//如果分析不出來就用的UTF-8
                            break;
                    }
                    // this.Literal1.Text = "Lenght:" + response.ContentLength.ToString() + "<br>CharacterSet:" + response.CharacterSet + "<br>Headers:" + response.Headers + "<br>";
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream, encoding);
                    responseFromServer = reader.ReadToEnd();
                    // this.TextBox2.Text = responseFromServer;
                    // FindLink(responseFromServer);
                    // this.TextBox2.Text = ClearHtml(responseFromServer);

                    reader.Close();
                    dataStream.Close();
                    response.Close();
                }
                else
                {
                    responseFromServer = "Error";
                }
            }
            catch (Exception ex) {
                throw ex;
            }
            return responseFromServer;
        }
        //取得標題有mega字眼的標題名稱和連結地址
        public Dictionary<string, string> FindLink(string html)
        {
            Dictionary<string, string> UrlList = new Dictionary<string, string>();
            try
            {
                string pattern = @"[<a\shref=""](?<url>thread-\d{8}-\d{1}-[A-Z0-9]{8}.html)[^>]*>(?<name>[^<]*(mega|MEGA|mu)[^<]*)</a>";

                foreach (Match match in Regex.Matches(html, pattern,RegexOptions.Multiline))
                {
                    if (match.Success && !UrlList.ContainsKey(match.Groups["name"].Value))
                    {
                        //加入集合數組
                        UrlList.Add(match.Groups["name"].Value, match.Groups["url"].Value);

                        // hrefList.Add(m.Groups["href"].Value);
                        //nameList.Add(m.Groups["name"].Value);
                        //      this.TextBox3.Text += m.Groups["href"].Value + "|" + m.Groups["name"].Value + "\n";
                    }
                }
            }
            catch (Exception e) {
                throw e;
            }
            return UrlList;
        }

        //取得第N頁資料
        public Dictionary<string, string> FindPage(string html)
        {
            Dictionary<string, string> UrlList = new Dictionary<string, string>();
            try
            {
                string pattern = @"[<a\shref=""](?<url>forum-\d{1,4}-[A-Z0-9]*.html)[^>]*>(?<page>\d{1,2})</a>";
                int result;
                foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Multiline))
                {
                    if (match.Success && !UrlList.ContainsKey(match.Groups["page"].Value) &&Int32.TryParse(match.Groups["page"].Value,out result))
                    {
                        //加入集合數組
                        UrlList.Add(match.Groups["page"].Value, match.Groups["url"].Value);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return UrlList;
        }

        //取得子頁面的下載連結和解壓密碼 
        public ResultModel GetDownloadLink(string html, string type)
        {
            Dictionary<string, string> UrlList = new Dictionary<string, string>();
            ResultModel Result = new ResultModel();
            Result.DownloadLink=new List<string>();
            try
            {
                string regFileSize = String.Empty;
                string regPassword = String.Empty;
                string regDownloadLink = @"(?<megaLink>https://mega[.co]?.nz/\#!?[a-zA-Z_!0-9-\#]*)";
                string pattern = @"訪客無法瀏覽下載點";
                if (Regex.IsMatch(html, pattern, RegexOptions.Multiline))
                {
                    throw new ArgumentException("登入失敗");
                }
                switch (type)
                {
                    case "成人影片":
                        regFileSize = @"影片大小[^0-9A-Za-z]*(?<filesize>[0-9A-Za-z.]*)[^0-9A-Za-z.]*";
                        regPassword = @"(?<password>【?(解壓縮碼|解壓密碼|密碼).*)<br\s/>";
                        
                        break;
                    case "電影下載區":
                        regFileSize = @"(影片大小|檔案大小)[^0-9A-Za-z]*(?<filesize>[0-9A-Za-z.]*)[^0-9A-Za-z.]*";
                        regPassword = @"(?<password>【?(解壓縮碼|解壓密碼|密碼).*)<br\s/>";
                        break;
                    case "遊戲下載區":
                        regFileSize = @"(影片大小|檔案大小)[^0-9A-Za-z]*(?<filesize>[0-9A-Za-z.]*)[^0-9A-Za-z.]*";
                        regPassword = @"(?<password>【?(解壓縮碼|解壓密碼|密碼).*)<br\s/>";
                        break;
                    case "電視劇下載區":
                        regFileSize = @"(影片大小|檔案大小)[^0-9A-Za-z]*(?<filesize>[0-9A-Za-z.]*)[^0-9A-Za-z.]*";
                        regPassword = @"(?<password>【?(解壓縮碼|解壓密碼|密碼).*)<br\s/>";
                        break;
                    default:
                        break;
                }
                //match檔案大小
                if (Regex.IsMatch(html, regFileSize))
                {
                    var filesize = Regex.Match(html, regFileSize);
                    Result.FileSize = filesize.Groups["filesize"].Value;
                }

                //match解壓密碼
                if (Regex.IsMatch(html, regPassword))
                {
                    var password = Regex.Match(html, regPassword);
                    Result.FilePassword = password.Groups["password"].Value;
                }

                //match Mega下載連結
                foreach (Match match in Regex.Matches(html, regDownloadLink, RegexOptions.Multiline))
                {
                    if (match.Success && !UrlList.ContainsKey(match.Groups["megaLink"].Value))
                    {
                        //加入集合數組
                        Result.DownloadLink.Add(match.Groups["megaLink"].Value);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return Result;
        }

        //清除html
        public string ClearHtml(string text)//過濾html,js,css代碼
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            text = Regex.Replace(text, "<head[^>]*>(?:.|[\r\n])*?</head>", "");
            text = Regex.Replace(text, "<script[^>]*>(?:.|[\r\n])*?</script>", "");
            text = Regex.Replace(text, "<style[^>]*>(?:.|[\r\n])*?</style>", "");

            text = Regex.Replace(text, "(<[b|B][r|R]/*>)+|(<[p|P](.|\\n)*?>)", ""); //<br> 
            text = Regex.Replace(text, "\\&[a-zA-Z]{1,10};", "");
            text = Regex.Replace(text, "<[^>]*>", "");

            text = Regex.Replace(text, "(\\s*&[n|N][b|B][s|S][p|P];\\s*)+", ""); //&nbsp;
            text = Regex.Replace(text, "<(.|\\n)*?>", string.Empty); //其它任何標記
            text = Regex.Replace(text, "[\\s]{2,}", " "); //兩個或多個空格替換為一個

            text = text.Replace("'", "''");
            text = text.Replace("\r\n", "");
            text = text.Replace("  ", "");
            text = text.Replace("\t", "");
            return text.Trim();
        }

       
        
    }
}
