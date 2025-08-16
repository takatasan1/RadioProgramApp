using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using RadioProgramApp.Models;

namespace RadioProgramApp.Models
{
    public class ParseRadikoStation
    {
        private readonly HttpClient _httpClient;
        private const string StationsListUrl = "https://radiko.jp/v3/station/region/full.xml";

        public ParseRadikoStation()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<Station>> GetStationsAsync()
        {
            var stationsList = new List<Station>(); // 変数名を stationsList に変更（単数形のstationと区別するため）

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(StationsListUrl);
                response.EnsureSuccessStatusCode();

                string xmlContent = await response.Content.ReadAsStringAsync();
                XDocument xmlDoc = XDocument.Parse(xmlContent);

                // XMLのルート要素 <region> から <stations> 要素のリストを取得
                var stationsGroupElements = xmlDoc.Root?.Elements("stations");

                if (stationsGroupElements == null)
                {
                    Console.WriteLine("No <stations> groups found in the XML.");
                    return stationsList; // 空のリストを返す
                }

                foreach (var stationsElement in stationsGroupElements)
                {
                    // <stations> タグの region_name 属性を取得
                    string regionName = stationsElement.Attribute("region_name")?.Value;

                    if (string.IsNullOrEmpty(regionName))
                    {
                        // region_name がない場合は、この<stations>グループをスキップするか、
                        // デフォルト値を設定するなどの対応を検討します。
                        // ここではコンソールに出力してスキップします。
                        Console.WriteLine($"Skipping a <stations> group because region_name is missing or empty. ASCII Name: {stationsElement.Attribute("ascii_name")?.Value}");
                        continue;
                    }

                    // 現在の <stations> 要素内の <station> 要素をすべて取得
                    var stationElements = stationsElement.Elements("station");

                    foreach (var stationElement in stationElements)
                    {
                        string id = stationElement.Element("id").Value;
                        string name = stationElement.Element("name").Value;
                        string bannerUrl = stationElement.Element("banner").Value; // <banner> タグの値を取得

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                        {
                            stationsList.Add(new Station
                            {
                                Id = id,
                                Name = name,
                                BannerUrl = bannerUrl,    // bannerのURLを設定
                                RegionName = regionName   // 親の<stations>から取得したregion_nameを設定
                            });
                        }
                        else
                        {
                            Console.WriteLine($"Skipping a <station> element due to missing id or name. (ID: {id}, Name: {name}) within Region: {regionName}");
                        }
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                throw;
            }
            catch (Exception e) // XmlParseExceptionなどもここでキャッチできる
            {
                Console.WriteLine($"An error occurred during XML parsing or processing: {e.Message}");
                throw;
            }

            return stationsList;
        }

        public async Task<List<ProgramInfo>> GetProgramScheduleAsync(string stationId, DateTime date)
        {
            var programList = new List<ProgramInfo>();
            string dateString = date.ToString("yyyyMMdd");
            string scheduleUrl = $"https://radiko.jp/v3/program/station/date/{dateString}/{stationId}.xml";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(scheduleUrl);
                response.EnsureSuccessStatusCode();

                string xmlContent = await response.Content.ReadAsStringAsync();
                XDocument xmlDoc = XDocument.Parse(xmlContent);

                var programElements = xmlDoc.Descendants("prog");

                foreach (var progElement in programElements)
                {
                    string id = progElement.Attribute("id")?.Value;
                    string title = progElement.Element("title")?.Value;
                    string performer = progElement.Element("pfm")?.Value;
                    string imageUrl = progElement.Element("img")?.Value;

                    string fullStartTime = progElement.Attribute("ft")?.Value; // yyyyMMddHHmmss
                    string fullEndTime = progElement.Attribute("to")?.Value;   // yyyyMMddHHmmss
                    string startTimeHHmm = progElement.Attribute("ftl")?.Value; // HHmm
                    string endTimeHHmm = progElement.Attribute("tol")?.Value;   // HHmm
                                                                                // ★ここから修正
                    string descHtmlContent = progElement.Element("desc")?.Value;
                    string infoHtmlContent = progElement.Element("info")?.Value;

                    string descPlainText = string.Empty;
                    string infoPlainText = string.Empty;

                    // <desc> からプレーンテキストを生成
                    if (!string.IsNullOrEmpty(descHtmlContent))
                    {
                        string tempText = Regex.Replace(descHtmlContent, "<.*?>", string.Empty);
                        tempText = System.Net.WebUtility.HtmlDecode(tempText);
                        descPlainText = Regex.Replace(tempText, @"\s+", " ").Trim();
                    }

                    // <info> からプレーンテキストを生成
                    if (!string.IsNullOrEmpty(infoHtmlContent))
                    {
                        string tempText = Regex.Replace(infoHtmlContent, "<.*?>", string.Empty);
                        tempText = System.Net.WebUtility.HtmlDecode(tempText);
                        infoPlainText = Regex.Replace(tempText, @"\s+", " ").Trim();
                    }

                    string finalSelectedHtmlContent = string.Empty;
                    string finalSelectedPlainText = string.Empty;

                    // プレーンテキストの長さを比較して、採用する方を決定
                    // descPlainText の方が長いか、同じ長さの場合 (descを優先)
                    if (descPlainText.Length >= infoPlainText.Length)
                    {
                        // ただし、descPlainText が実際に内容を持つ場合のみ採用
                        if (!string.IsNullOrEmpty(descPlainText))
                        {
                            finalSelectedPlainText = descPlainText;
                            finalSelectedHtmlContent = descHtmlContent;
                        }
                        // descPlainTextが空でも、infoPlainTextに内容があればそちらを採用
                        else if (!string.IsNullOrEmpty(infoPlainText))
                        {
                            finalSelectedPlainText = infoPlainText;
                            finalSelectedHtmlContent = infoHtmlContent;
                        }
                        // 両方ともプレーンテキストが空の場合は、どちらも採用しない (finalSelected... は空のまま)
                    }
                    else // infoPlainText の方が長い場合
                    {
                        finalSelectedPlainText = infoPlainText;
                        finalSelectedHtmlContent = infoHtmlContent;
                    }
                    // ★ここまで修正

                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(fullStartTime) && !string.IsNullOrEmpty(fullEndTime))
                    {
                        programList.Add(new ProgramInfo
                        {
                            Id = id,
                            Title = title,
                            Performer = performer,
                            ImageUrl = imageUrl,
                            FullStartTime = fullStartTime,
                            FullEndTime = fullEndTime,
                            StartTimeHHmm = startTimeHHmm,
                            EndTimeHHmm = endTimeHHmm,
                            InfoHtml = finalSelectedHtmlContent, // 元のHTMLも保存
                            InfoText = finalSelectedPlainText    // タグ除去後のテキスト
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Skipping a <prog> element due to missing essential data. ID: {id}, Station: {stationId}, Date: {dateString}");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error fetching schedule for {stationId} on {dateString}: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing schedule XML for {stationId} on {dateString}: {e.Message}");
                throw;
            }

            return programList;
        }
    }
}
