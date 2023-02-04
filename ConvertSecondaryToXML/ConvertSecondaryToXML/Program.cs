using ConvertSecondaryToXML.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace ConvertSecondaryToXML
{
    class Program
    {


        private global_Session ObjSesssion = global_Session.OnceCreateInstance;
        static void Main(string[] args)
        {

            string IncomingPath = ConfigurationManager.AppSettings.Get("IncomingPath");
            string OutputPath = ConfigurationManager.AppSettings.Get("OutputPath");
            string ErrorLogPath = ConfigurationManager.AppSettings.Get("ErrorLogPath");
            List<string> ProductionCourtList = ConfigurationManager.AppSettings.Get("ProductionCourt").Split(',').ToList();
            List<string> ProductionStateList = ConfigurationManager.AppSettings.Get("ProductionState").Split(',').ToList();
            string ErrorLogFile = Path.Combine(ErrorLogPath, "ConversionError-" + DateTime.Now.ToString("MMddyyyy-hhmmss") + ".txt");
            int IdealTime = 600;

            ConvertHtmlToXml(IncomingPath, OutputPath, ErrorLogPath, ProductionCourtList, ProductionStateList, ErrorLogFile, IdealTime);

            //  ConvertXMLToXml(IncomingPath, OutputPath, ErrorLogPath, ProductionCourtList, ProductionStateList, ErrorLogFile, IdealTime);

        }


        // convert Data from html to xml

        static void ConvertXMLToXml(string IncomingPath, string OutputPath, string ErrorLogPath, List<string> ProductionCourtList, List<string> ProductionStateList, string ErrorLogFile, int IdealTime)
        {
            List<string> FilesList = GetAllFile_2(IncomingPath);
            int CaseCounter = 1;

            List<Opinion> AllOpinionData = new List<Opinion>();
            foreach (string File in FilesList)
            {

                Console.WriteLine("[" + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + "] Converting " + File + " => ");
                string Content = string.Empty;
                string LibraryName = "Washington Shidler Journal of Law, Commerce and Technology";
                List<string> DocketList = new List<string>();
                byte[] Bytes = System.IO.File.ReadAllBytes(File);
                SimpleHelpers.FileEncoding CheckEncoding = new SimpleHelpers.FileEncoding();
                string Encode = CheckEncoding.Detect(Bytes, 0, Bytes.Length);
                if (string.IsNullOrEmpty(Encode))
                {
                    //Content = Encoding.UTF8.GetString(Bytes);
                    byte[] UTF8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Bytes);
                    Content = Encoding.UTF8.GetString(UTF8Bytes);
                }
                else
                {
                    byte[] UTF8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Bytes);
                    Content = Encoding.UTF8.GetString(UTF8Bytes);
                }
                Content = Content.Replace("&lt;--OPINION TEXT STARTS HERE//--&gt;", "<!--OPINION TEXT STARTS HERE//-->");
                Content = Regex.Replace(Content, "Â", "");

                if (Content != "")
                {
                    if (Content.IndexOf("<code type=\"Root\">") > -1)
                    {
                        string rootData = Content.Substring(Content.IndexOf("<code type=\"Root\">"));

                        if (rootData.IndexOf("<code type=\"Volume\">") > -1)
                        {
                            string beforeVolumeData = rootData.Substring(0, rootData.IndexOf("<code type=\"Volume\">"));
                            string afterRootData = rootData.Substring(rootData.IndexOf("<code type=\"Volume\">"));
                            string volumeData = afterRootData.Substring(0, afterRootData.IndexOf("<code type=\"Opinion\""));
                            string afterVolumeData = rootData.Substring(rootData.IndexOf("<code type=\"Opinion\""));

                            string volumeNumber = Regex.Match(volumeData, "<number>(.*?)</number>", RegexOptions.IgnoreCase).Groups[1].Value;
                            string[] allOpinionData = Regex.Split(afterVolumeData, "<code\\s+type=\"Opinion\"", RegexOptions.IgnoreCase);

                            string FullStateName = GetJSStateNameLibra("WA");
                            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                            for (int i = 0; i < allOpinionData.Length; i++)
                            {
                                string opinionData = allOpinionData[i].ToString();

                                if (opinionData != "")
                                {
                                    Opinion objOpinionData = new Opinion();

                                    string numberData = opinionData.Substring(opinionData.IndexOf("<number>"));
                                    numberData = numberData.Substring(0, numberData.IndexOf("</number>"));
                                    numberData = numberData.Replace("<number>", "");
                                    string nameData = opinionData.Substring(opinionData.IndexOf("<name>"));
                                    nameData = nameData.Substring(0, nameData.IndexOf("</name>"));
                                    nameData = nameData.Replace("<name>", "");
                                    string contentData = opinionData.Substring(opinionData.IndexOf("<content>"));
                                    contentData = contentData.Substring(0, contentData.LastIndexOf("</content>"));


                                    objOpinionData.ShortName = "Volume " + volumeNumber;
                                    objOpinionData.DecisionDate = "01/01/2021";
                                    objOpinionData.Author = "";
                                    objOpinionData.PartyHeader = "";
                                    objOpinionData.OpinionHtml = contentData;
                                    objOpinionData.HeaderHtml = "";
                                    objOpinionData.LibraryName = LibraryName;
                                    objOpinionData.CourtAbbreviation = "";
                                    objOpinionData.SaveFolderPath = SavePath;
                                    objOpinionData.VolumeName = "";
                                    objOpinionData.Caption = "";
                                    objOpinionData.Description = "";
                                    objOpinionData.FilePath = File;
                                    objOpinionData.Browslevel1 = "Volume " + volumeNumber;
                                    objOpinionData.Browslevel2 = numberData + " " + nameData;

                                    AllOpinionData.Add(objOpinionData);
                                }

                            }


                        }

                    }
                }

            }

            if (AllOpinionData != null)
            {
                // AllHTMLData = AllHTMLData.OrderBy(O => O.CodeTitle).ToList();
                AllOpinionData = AllOpinionData.OrderByDescending(O => O.ShortName).ToList();
                //  AllHTMLData = AllHTMLData.OrderByDescending(O => Convert.ToInt32(O.SortOrderValue)).ToList();
                foreach (Opinion ObjItem in AllOpinionData)
                {
                    SaveOpinionInXml_New2(ObjItem, ObjItem.FilePath, IncomingPath, OutputPath, ErrorLogFile);

                }
            }

        }
        static void ConvertHtmlToXml(string IncomingPath, string OutputPath, string ErrorLogPath, List<string> ProductionCourtList, List<string> ProductionStateList, string ErrorLogFile, int IdealTime)
        {
            List<string> FilesList = new List<string>();
            data_model objData = new data_model();

            //code to take for federal register
            if (IncomingPath.IndexOf(@"FederalRegister", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                List<federal_date_data_model> AllFederalList = objData.GetAllFile(IncomingPath, ErrorLogPath, "2023", "2023");
                Federal_Conversion(AllFederalList, IncomingPath, OutputPath, ErrorLogPath, ProductionCourtList, ProductionStateList, ErrorLogFile, IdealTime);

            }
            else
            {
                //Code to take for general
                FilesList = GetAllFile(IncomingPath);
                generalConversion(FilesList, IncomingPath, OutputPath, ErrorLogPath, ProductionCourtList, ProductionStateList, ErrorLogFile, IdealTime);

            }


        }
        //Created By Santosh


        static void Federal_Conversion(List<federal_date_data_model> AllFederalList, string IncomingPath, string OutputPath, string ErrorLogPath, List<string> ProductionCourtList, List<string> ProductionStateList, string ErrorLogFile, int IdealTime)
        {

            int CaseCounter = 1;
            string MainPath = "";
            if (IncomingPath.IndexOf("Federal Register") > 0)
            {
                MainPath = IncomingPath.Substring(0, IncomingPath.IndexOf("Federal Register")) + "Federal Register";
            }
            else {

                MainPath = IncomingPath;
            }




            foreach (federal_date_data_model ObjFedDate in AllFederalList)
            {
                foreach (federal_register_model ObjFed in ObjFedDate.allFederalData)
                {
                    string File1 = "";

                    if (ObjFed.filepath != null)
                    {
                        if (ObjFed.filepath.IndexOf("FederalRegister") > 0)
                        {
                            string filePath = ObjFed.filepath.Substring(ObjFed.filepath.IndexOf("FederalRegister") + ("FederalRegister").Length);
                            string fullPath = MainPath + filePath;
                            if (File.Exists(fullPath))
                            {
                                File1 = fullPath;
                            }
                            else
                            {
                                File.AppendAllText(ErrorLogPath + "\\filenotfound.txt", "File Not Found in Physical Folder=" + fullPath + "\n");
                                continue;
                            }
                        }
                        else
                        {
                            File.AppendAllText(ErrorLogPath + "\\filenotfound.txt", "File Different in database=" + ObjFed.filepath + "\n");
                            continue;
                        }
                    }
                    else
                    {
                        File.AppendAllText(ErrorLogPath + "\\filenotfound.txt", "File Different in database=" + ObjFed.filepath + "\n");
                        continue;
                    }



                    Console.WriteLine("[" + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + "] Converting " + File1 + " => ");
                    Opinion XMLVersion = new Opinion();
                    html_meta_model AllMetaValue = new html_meta_model();

                    string Content = string.Empty;
                    List<string> DocketList = new List<string>();
                    byte[] Bytes = System.IO.File.ReadAllBytes(File1);
                    SimpleHelpers.FileEncoding CheckEncoding = new SimpleHelpers.FileEncoding();
                    string Encode = CheckEncoding.Detect(Bytes, 0, Bytes.Length);
                    if (string.IsNullOrEmpty(Encode))
                    {
                        //Content = Encoding.UTF8.GetString(Bytes);
                        byte[] UTF8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Bytes);
                        Content = Encoding.UTF8.GetString(UTF8Bytes);
                    }
                    else
                    {
                        byte[] UTF8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Bytes);
                        Content = Encoding.UTF8.GetString(UTF8Bytes);
                    }
                    Content = Content.Replace("&lt;--OPINION TEXT STARTS HERE//--&gt;", "<!--OPINION TEXT STARTS HERE//-->");

                    Content = Content.Replace("â€œ", "\"");
                    Content = Content.Replace("â€‰", " ");
                    Content = Content.Replace("â€", "\"");
                    Content = Content.Replace("ï»¿", "");
                    Content = Content.Replace("Â", "");
                    Content = Content.Replace("\"¢", "&bull;");


                    FileInfo FInfo = new FileInfo(File1);

                    string FileName = FInfo.Name;

                    if (FileName.IndexOf("+") > 0)
                    {
                        continue;
                    }

                    string Caption = string.Empty;
                    string Description = string.Empty;
                    string ShortName = string.Empty;
                    string VolumeName = string.Empty;
                    string Browslevel1 = string.Empty;
                    string Browslevel2 = string.Empty;
                    string Browslevel3 = string.Empty;
                    string Browslevel4 = string.Empty;

                    string DocketNumber = string.Empty;
                    string Author = string.Empty;
                    string PartyHeader = string.Empty;
                    string HeaderHtml = GetHeader(Content);
                    string OpinionHtml = GetOpinion(Content);
                    string LibraryName = string.Empty;
                    string CourtAbbreviation = string.Empty;
                    string DecisionDate = string.Empty;
                    string SavePath = string.Empty;
                    string replacematt = string.Empty;
                    string replacematt_final = string.Empty;
                    OpinionHtml = Regex.Replace(OpinionHtml, "Â", "");
                    OpinionHtml = Regex.Replace(OpinionHtml, "¶", "¶");

                    //************************************************************               


                    #region Common Function To Get FEDREG Information From File 

                    if (FInfo.FullName.IndexOf(@"FederalRegister", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        HeaderHtml = " ";
                        HeaderOpinion objFEDREG = common_FEDREG_HeaderInfo(Content, OutputPath, CaseCounter.ToString(), ObjFed);
                        if (objFEDREG.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                        {
                            DecisionDate = objFEDREG.DecisionDate.Substring(objFEDREG.DecisionDate.LastIndexOf("|") + 1);
                            string Log = File1 + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                            File.AppendAllText(ErrorLogFile, Log);
                            continue;
                        }
                        bool flagValidateDate = validateDate(Convert.ToDateTime(objFEDREG.DecisionDate.Trim()));
                        if (flagValidateDate == false)
                        {
                            DecisionDate = objFEDREG.DecisionDate.Substring(objFEDREG.DecisionDate.LastIndexOf("|") + 1);
                            string Log = File1 + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                            File.AppendAllText(ErrorLogFile, Log);
                            continue;
                        }
                        ShortName = objFEDREG.ShortName;
                        DocketNumber = objFEDREG.DocketNumber;
                        DecisionDate = objFEDREG.DecisionDate;
                        PartyHeader = objFEDREG.PartyHeader;
                        LibraryName = objFEDREG.LibraryName;
                        SavePath = objFEDREG.SaveFolderPath;
                        VolumeName = objFEDREG.VolumeName;
                        Caption = ObjFed.frCitation;
                        Description = " - " + ObjFed.agencyName;
                        Browslevel1 = objFEDREG.Browslevel1;
                        Browslevel2 = objFEDREG.Browslevel2;
                        Browslevel3 = ObjFedDate.short_name;

                    }

                    #endregion



                    if (HeaderHtml == "" || OpinionHtml == "")
                    {
                        string Log = File1 + "|" + "Header or Opinion not found\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        //MoveSkipedFile(IncomingPath, File);
                        continue;
                    }
                    else
                    {
                        if (HeaderHtml == "temporaryheader")
                        {
                            HeaderHtml = "";
                        }

                        XMLVersion.ShortName = ShortName;
                        if (DocketNumber != "")
                        {
                            XMLVersion.DocketNumbers = new List<Docket>();
                            if (DocketNumber.IndexOf(", ") > 0 && LibraryName != "Colorado Lawyer")
                            { DocketList = DocketNumber.Split(',').ToList(); }
                            else if (DocketNumber.IndexOf("; ") > 0)
                            { DocketList = DocketNumber.Split(';').ToList(); }
                            else
                            { DocketList.Add(DocketNumber); }
                            foreach (string DocketNo in DocketList)
                            {
                                if (DocketNo.Trim() != "")
                                { XMLVersion.DocketNumbers.Add(new Docket(DocketNo.Trim())); }
                            }
                            DocketList.Clear();
                            foreach (Docket DocketNo in XMLVersion.DocketNumbers)
                            {
                                DocketList.Add(DocketNo.DocketNumber);
                            }
                        }
                        XMLVersion.DecisionDate = DecisionDate;
                        XMLVersion.Author = Author;
                        XMLVersion.PartyHeader = PartyHeader;
                        XMLVersion.OpinionHtml = OpinionHtml;
                        XMLVersion.HeaderHtml = HeaderHtml;
                        XMLVersion.LibraryName = LibraryName;
                        XMLVersion.CourtAbbreviation = CourtAbbreviation;
                        XMLVersion.SaveFolderPath = SavePath;
                        XMLVersion.VolumeName = VolumeName;
                        XMLVersion.Caption = Caption;
                        XMLVersion.Description = Description;
                        XMLVersion.Browslevel1 = Browslevel1;
                        XMLVersion.Browslevel2 = Browslevel2;
                        XMLVersion.Browslevel3 = Browslevel3;

                        //---use below code when need to create hierarchy for fastcase 

                        SaveOpinionInXml_New(XMLVersion, File1, IncomingPath, OutputPath, ErrorLogFile);
                        //global_Session._global_instance.xmlDocument.Save(global_Session._global_instance.savepath);
                        //-----------------End--------------------


                        CaseCounter += 1;
                    }


                }

            }
            if (global_Session._global_instance.savepath != null)
            {
                global_Session._global_instance.xmlDocument.Save(global_Session._global_instance.savepath);
                global_Session._global_instance.xmlDocument = null;

                

            }


            


        }


        static void generalConversion(List<string> FilesList, string IncomingPath, string OutputPath, string ErrorLogPath, List<string> ProductionCourtList, List<string> ProductionStateList, string ErrorLogFile, int IdealTime)
        {

            int CaseCounter = 1;

            List<html_meta_model> AllHTMLData = new List<html_meta_model>();
            foreach (string File in FilesList)
            {

                Console.WriteLine("[" + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss") + "] Converting " + File + " => ");
                Opinion XMLVersion = new Opinion();
                html_meta_model AllMetaValue = new html_meta_model();

                string Content = string.Empty;
                List<string> DocketList = new List<string>();
                byte[] Bytes = System.IO.File.ReadAllBytes(File);
                SimpleHelpers.FileEncoding CheckEncoding = new SimpleHelpers.FileEncoding();
                string Encode = CheckEncoding.Detect(Bytes, 0, Bytes.Length);
                if (string.IsNullOrEmpty(Encode))
                {
                    //Content = Encoding.UTF8.GetString(Bytes);
                    byte[] UTF8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Bytes);
                    Content = Encoding.UTF8.GetString(UTF8Bytes);
                }
                else
                {
                    byte[] UTF8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Bytes);
                    Content = Encoding.UTF8.GetString(UTF8Bytes);
                }
                Content = Content.Replace("&lt;--OPINION TEXT STARTS HERE//--&gt;", "<!--OPINION TEXT STARTS HERE//-->");

                Content = Content.Replace("â€œ", "\"");
                Content = Content.Replace("â€‰", " ");
                Content = Content.Replace("â€", "\"");
                Content = Content.Replace("ï»¿", "");
                Content = Content.Replace("Â", "");
                Content = Content.Replace("\"¢", "&bull;");
                Content = Content.Replace("\"”", "—");
                Content = Content.Replace("\"“", "—");
                Content = Content.Replace("\"™", "’");
                Content = Content.Replace("\"™", "’");
                Content = Content.Replace("â—", "&bull;");
                


                FileInfo FInfo = new FileInfo(File);

                string FileName = FInfo.Name;

                if (FileName.IndexOf("+") > 0)
                {
                    continue;
                }

                string Caption = string.Empty;
                string Description = string.Empty;
                string ShortName = string.Empty;
                string VolumeName = string.Empty;
                string Browslevel1 = string.Empty;
                string Browslevel2 = string.Empty;
                string Browslevel3 = string.Empty;
                string Browslevel4 = string.Empty;

                string DocketNumber = string.Empty;
                string Author = string.Empty;
                string PartyHeader = string.Empty;
                string HeaderHtml = GetHeader(Content);
                string OpinionHtml = GetOpinion(Content);
                string LibraryName = string.Empty;
                string CourtAbbreviation = string.Empty;
                string DecisionDate = string.Empty;
                string SavePath = string.Empty;
                int MetaSt = 0;
                int MetaEnd = 0;
                string replacematt = string.Empty;
                string replacematt_final = string.Empty;
                OpinionHtml = Regex.Replace(OpinionHtml, "Â", "");
                OpinionHtml = Regex.Replace(OpinionHtml, "¶", "¶");

                //************************************************************               
                if (Content.Trim().IndexOf(@"<style type=""text/css"">", StringComparison.OrdinalIgnoreCase) != -1 &&
                (Content.Trim().IndexOf("span.c1 {text-decoration: underline}") != -1 ||
                Content.Trim().IndexOf("span.c2 {text-decoration: underline}") != -1))
                {
                    MetaSt = OpinionHtml.IndexOf("<span", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf(">", MetaSt) + 1;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c1\"") != -1 || replacematt.IndexOf("\"c2\"") != -1)
                                {
                                    OpinionHtml = Regex.Replace(OpinionHtml, replacematt, "<u>", RegexOptions.IgnoreCase);
                                    MetaSt = OpinionHtml.IndexOf("<span", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<span", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                        OpinionHtml = Regex.Replace(OpinionHtml, "</span>", "</u>", RegexOptions.IgnoreCase);
                    }
                }

                replacematt_final = "";
                if (Content.Trim().IndexOf(@"<style type=""text/css"">", StringComparison.OrdinalIgnoreCase) != -1 &&
                (Content.Trim().IndexOf("p.c1 {text-decoration: underline}") != -1 ||
                Content.Trim().IndexOf("p.c2 {text-decoration: underline}") != -1))
                {
                    MetaSt = OpinionHtml.IndexOf("<p ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</p>", MetaSt) + 4;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if ((replacematt.IndexOf("\"c1\"") != -1 || replacematt.IndexOf("\"c2\"") != -1) && replacematt.IndexOf("<a ") == -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</p>", "</u>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<u>" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);
                                    //OpinionHtml = Regex.Replace(OpinionHtml, replacematt, replacematt_final, RegexOptions.IgnoreCase);
                                    MetaSt = OpinionHtml.IndexOf("<p", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<p", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }


                if (Content.Trim().IndexOf(@"<style type=""text/css"">", StringComparison.OrdinalIgnoreCase) != -1 &&
                    Content.Trim().IndexOf("div.c1 {text-align: center}") != -1)
                {
                    replacematt_final = "";
                    MetaSt = OpinionHtml.IndexOf("<div ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</div>", MetaSt, StringComparison.InvariantCultureIgnoreCase) + 6;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c1\"") != -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</div>", "</center>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<center>" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);
                                    MetaSt = OpinionHtml.IndexOf("<div", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<div", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }

                if (Content.Trim().IndexOf(@"p.c1 {text-align: center; text-decoration: underline}", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    MetaSt = OpinionHtml.IndexOf("<p ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</p>", MetaSt) + 4;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c1\"") != -1 && replacematt.IndexOf("<a ") == -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</p>", "</u></center></p>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<p><center><u>" + replacematt_final;

                                    OpinionHtml = Regex.Replace(OpinionHtml, replacematt, replacematt_final, RegexOptions.IgnoreCase);
                                    MetaSt = OpinionHtml.IndexOf("<p", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<p", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }

                if (Content.Trim().IndexOf(@"p.c2 {text-align: center; text-decoration: underline}", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    MetaSt = OpinionHtml.IndexOf("<p ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</p>", MetaSt) + 4;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c2\"") != -1 && replacematt.IndexOf("<a ") == -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</p>", "</u></center>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<center><u>" + replacematt_final;

                                    OpinionHtml = Regex.Replace(OpinionHtml, replacematt, replacematt_final, RegexOptions.IgnoreCase);
                                    MetaSt = OpinionHtml.IndexOf("<p ", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<p ", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }

                if (Content.Trim().IndexOf(@"span.c1 {text-align: center; text-decoration: underline}", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    replacematt_final = "";
                    MetaSt = OpinionHtml.IndexOf("<div ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</div>", MetaSt) + 6;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c1\"") != -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</div>", "</u></center>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<center><u>" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);
                                    MetaSt = OpinionHtml.IndexOf("<div", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<div", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }

                if (Content.Trim().IndexOf(@"p.c2 {text-align: center; text-decoration: underline}", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    replacematt_final = "";
                    MetaSt = OpinionHtml.IndexOf("<div ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</div>", MetaSt) + 6;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c2\"") != -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</div>", "</u></center>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<center><u>" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);
                                    MetaSt = OpinionHtml.IndexOf("<div", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<div", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }

                if (Content.Trim().IndexOf(@"p.c1 {text-align: center}", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    MetaSt = OpinionHtml.IndexOf("<p ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</p>", MetaSt) + 4;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c1\"") != -1 && replacematt.IndexOf("<a ") == -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</p>", "</center></p>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<p><center>" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);
                                    //OpinionHtml = Regex.Replace(OpinionHtml, replacematt, replacematt_final, RegexOptions.IgnoreCase);
                                    MetaSt = OpinionHtml.IndexOf("<p", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<p", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }

                if (Content.Trim().IndexOf(@"p.c2 {text-align: center}", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    MetaSt = OpinionHtml.IndexOf("<p ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</p>", MetaSt) + 4;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c2\"") != -1 && replacematt.IndexOf("<a ") == -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</p>", "</center></p>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<p><center>" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);

                                    // OpinionHtml = Regex.Replace(OpinionHtml, replacematt, replacematt_final, RegexOptions.IgnoreCase);
                                    MetaSt = OpinionHtml.IndexOf("<p", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<p", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }

                if (Content.Trim().IndexOf(@"span.c2 {text-decoration: line-through}", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    MetaSt = OpinionHtml.IndexOf("<span ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</span>", MetaSt) + 7;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("\"c2\"") != -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</span>", "</strike></span>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<span><strike>" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);
                                    MetaSt = OpinionHtml.IndexOf("<span", MetaSt + 8, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<span", MetaSt + 8, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }




                //For NE LR merging issue starts here
                if (Content.Trim().IndexOf("<DIV ALIGN=\"CENTER\">", StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    replacematt_final = "";
                    MetaSt = OpinionHtml.IndexOf("<div ", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</div>", MetaSt, StringComparison.InvariantCultureIgnoreCase) + 6;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.Trim().IndexOf("<DIV ALIGN=\"CENTER\">", StringComparison.CurrentCultureIgnoreCase) != -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</div>", "</P>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<P ALIGN=\"CENTER\">" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);
                                    MetaSt = OpinionHtml.IndexOf("<div", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<div", MetaSt + 5, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }
                //For NE LR merging issue ends here

                //For blockquote spacing issue starts
                if (Content.Trim().IndexOf("<blockquote", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    replacematt_final = "";
                    MetaSt = OpinionHtml.IndexOf("<blockquote", StringComparison.InvariantCultureIgnoreCase);
                    if (MetaSt >= 0)
                    {
                        do
                        {
                            MetaEnd = OpinionHtml.IndexOf("</blockquote>", MetaSt, StringComparison.OrdinalIgnoreCase) + 13;
                            if (MetaEnd >= 0)
                            {
                                replacematt = OpinionHtml.Substring(MetaSt, MetaEnd - MetaSt);
                                if (replacematt.IndexOf("<p") == -1)
                                {
                                    replacematt_final = replacematt.Substring(replacematt.IndexOf(">") + 1);
                                    replacematt_final = Regex.Replace(replacematt_final, "</blockquote>", "</p></blockquote>", RegexOptions.IgnoreCase);
                                    replacematt_final = "<blockquote><p>" + replacematt_final;

                                    OpinionHtml = OpinionHtml.Replace(replacematt, replacematt_final);
                                    MetaSt = OpinionHtml.IndexOf("<blockquote", MetaSt + 12, StringComparison.InvariantCultureIgnoreCase);
                                }
                                else
                                {
                                    MetaSt = OpinionHtml.IndexOf("<blockquote", MetaSt + 12, StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        } while (MetaSt >= 0);
                    }
                }
                //For blockquote spacing issue end





                OpinionHtml = Regex.Replace(OpinionHtml, "<em>", "<i>", RegexOptions.IgnoreCase);
                OpinionHtml = Regex.Replace(OpinionHtml, "</em>", "</i>", RegexOptions.IgnoreCase);
                OpinionHtml = Regex.Replace(OpinionHtml, "<strong>", "<b>", RegexOptions.IgnoreCase);
                OpinionHtml = Regex.Replace(OpinionHtml, "</strong>", "</b>", RegexOptions.IgnoreCase);
                if (FInfo.FullName.IndexOf(@"\Michigan Attorney General Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    OpinionHtml = Regex.Replace(OpinionHtml, @"<td>(\s|\n)*<p>", @"<td><p style=""margin-right: 1em;"">", RegexOptions.IgnoreCase);
                }

                if (FInfo.FullName.IndexOf(@"\Vermont Workers Compensation Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    OpinionHtml = Regex.Replace(OpinionHtml, @"<table>", @"<table border=""1"">", RegexOptions.IgnoreCase);
                }

                //************************************************************

                #region The New Hampshire Commission For Human Rights(NHCHR)

                if (FInfo.FullName.IndexOf(@"New Hampshire Commission For Human Rights", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objNHCHR = common_NHCHR_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objNHCHR.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objNHCHR.DecisionDate.Substring(objNHCHR.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objNHCHR.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objNHCHR.DecisionDate.Substring(objNHCHR.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objNHCHR.ShortName;
                    DocketNumber = objNHCHR.DocketNumber;
                    DecisionDate = objNHCHR.DecisionDate;
                    PartyHeader = objNHCHR.PartyHeader;
                    LibraryName = objNHCHR.LibraryName;
                    SavePath = objNHCHR.SaveFolderPath;
                    VolumeName = objNHCHR.VolumeName;

                }


                #endregion


                #region FAA(Federal Aviation Administration Office of the Chief Counsel)


                if (FInfo.FullName.IndexOf(@"Federal Aviation Administration Office", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objFAA = common_FAA_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objFAA.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objFAA.DecisionDate.Substring(objFAA.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objFAA.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objFAA.DecisionDate.Substring(objFAA.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objFAA.ShortName;
                    DocketNumber = objFAA.DocketNumber;
                    DecisionDate = objFAA.DecisionDate;
                    PartyHeader = objFAA.PartyHeader;
                    LibraryName = objFAA.LibraryName;
                    SavePath = objFAA.SaveFolderPath;
                    VolumeName = objFAA.VolumeName;

                }

                #endregion


                #region HICIVRCOM
                if (FInfo.FullName.IndexOf(@"HICIVRCOM", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objHICIVRCOM = common_HICIVRCOM_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objHICIVRCOM.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objHICIVRCOM.DecisionDate.Substring(objHICIVRCOM.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objHICIVRCOM.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objHICIVRCOM.DecisionDate.Substring(objHICIVRCOM.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objHICIVRCOM.ShortName;
                    DocketNumber = objHICIVRCOM.DocketNumber;
                    DecisionDate = objHICIVRCOM.DecisionDate;
                    PartyHeader = objHICIVRCOM.PartyHeader;
                    LibraryName = objHICIVRCOM.LibraryName;
                    SavePath = objHICIVRCOM.SaveFolderPath;
                    VolumeName = objHICIVRCOM.VolumeName;

                }



                #endregion


                #region INTERNAL_REVENUE_BULLETIN

                if (FInfo.FullName.IndexOf(@"Internal Revenue Bulletin", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderHtml = "temporaryheader";

                    HeaderOpinion objIRD = common_IRD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objIRD.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objIRD.DecisionDate.Substring(objIRD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objIRD.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objIRD.DecisionDate.Substring(objIRD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objIRD.ShortName;
                    DocketNumber = objIRD.DocketNumber;
                    DecisionDate = objIRD.DecisionDate;
                    PartyHeader = objIRD.PartyHeader;
                    LibraryName = objIRD.LibraryName;
                    SavePath = objIRD.SaveFolderPath;
                    VolumeName = objIRD.VolumeName;

                }


                #endregion


                #region Internal Revenue Service(US)

                if (FInfo.FullName.IndexOf(@"Internal Revenue Service", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderHtml = "temporaryheader";

                    HeaderOpinion objIRD = common_IRS_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objIRD.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objIRD.DecisionDate.Substring(objIRD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objIRD.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objIRD.DecisionDate.Substring(objIRD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objIRD.ShortName;
                    DocketNumber = objIRD.DocketNumber;
                    DecisionDate = objIRD.DecisionDate;
                    PartyHeader = objIRD.PartyHeader;
                    LibraryName = objIRD.LibraryName;
                    SavePath = objIRD.SaveFolderPath;
                    VolumeName = objIRD.VolumeName;

                }


                #endregion

                #region MSPSC

                if (FInfo.FullName.IndexOf(@"MSPSC", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objMSPSC = common_MSPSC_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objMSPSC.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objMSPSC.DecisionDate.Substring(objMSPSC.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objMSPSC.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objMSPSC.DecisionDate.Substring(objMSPSC.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objMSPSC.ShortName;
                    DocketNumber = objMSPSC.DocketNumber;
                    DecisionDate = objMSPSC.DecisionDate;
                    PartyHeader = objMSPSC.PartyHeader;
                    LibraryName = objMSPSC.LibraryName;
                    SavePath = objMSPSC.SaveFolderPath;
                    VolumeName = objMSPSC.VolumeName;

                }

                #endregion



                #region KY MCAIDORD 

                if (FInfo.FullName.IndexOf(@"Administrative Hearings Branch Orders", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objMCAIDORD = common_MCAIDORD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objMCAIDORD.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objMCAIDORD.DecisionDate.Substring(objMCAIDORD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objMCAIDORD.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objMCAIDORD.DecisionDate.Substring(objMCAIDORD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objMCAIDORD.ShortName;
                    DocketNumber = objMCAIDORD.DocketNumber;
                    DecisionDate = objMCAIDORD.DecisionDate;
                    PartyHeader = objMCAIDORD.PartyHeader;
                    LibraryName = objMCAIDORD.LibraryName;
                    SavePath = objMCAIDORD.SaveFolderPath;
                    VolumeName = objMCAIDORD.VolumeName;

                }
                #endregion

                #region KY KYOMD 

                if (FInfo.FullName.IndexOf(@"Open Meetings Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objKYOMD = common_KYOMD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objKYOMD.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objKYOMD.DecisionDate.Substring(objKYOMD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objKYOMD.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objKYOMD.DecisionDate.Substring(objKYOMD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objKYOMD.ShortName;
                    DocketNumber = objKYOMD.DocketNumber;
                    DecisionDate = objKYOMD.DecisionDate;
                    PartyHeader = objKYOMD.PartyHeader;
                    LibraryName = objKYOMD.LibraryName;
                    SavePath = objKYOMD.SaveFolderPath;
                    VolumeName = objKYOMD.VolumeName;
                }

                #endregion

                #region KY KYORD 

                if (FInfo.FullName.IndexOf(@"Open Records Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objKYORD = common_KYORD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objKYORD.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objKYORD.DecisionDate.Substring(objKYORD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objKYORD.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objKYORD.DecisionDate.Substring(objKYORD.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objKYORD.ShortName;
                    DocketNumber = objKYORD.DocketNumber;
                    DecisionDate = objKYORD.DecisionDate;
                    PartyHeader = objKYORD.PartyHeader;
                    LibraryName = objKYORD.LibraryName;
                    SavePath = objKYORD.SaveFolderPath;
                    VolumeName = objKYORD.VolumeName;
                }

                #endregion


                #region  AdminCourt

                if (FInfo.FullName.IndexOf(@"Administrative Court Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objAdminCourt = common_AdminCourt_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objAdminCourt.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objAdminCourt.DecisionDate.Substring(objAdminCourt.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objAdminCourt.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objAdminCourt.DecisionDate.Substring(objAdminCourt.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objAdminCourt.ShortName;
                    DocketNumber = objAdminCourt.DocketNumber;
                    DecisionDate = objAdminCourt.DecisionDate;
                    PartyHeader = objAdminCourt.PartyHeader;
                    LibraryName = objAdminCourt.LibraryName;
                    SavePath = objAdminCourt.SaveFolderPath;
                    VolumeName = objAdminCourt.VolumeName;
                }

                #endregion


                #region  FOI

                if (FInfo.FullName.IndexOf(@"FOI Com Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objFOI = common_FOI_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objFOI.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objFOI.DecisionDate.Substring(objFOI.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objFOI.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objFOI.DecisionDate.Substring(objFOI.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objFOI.ShortName;
                    DocketNumber = objFOI.DocketNumber;
                    DecisionDate = objFOI.DecisionDate;
                    PartyHeader = objFOI.PartyHeader;
                    LibraryName = objFOI.LibraryName;
                    SavePath = objFOI.SaveFolderPath;
                    VolumeName = objFOI.VolumeName;
                }

                #endregion

                #region Tax Court Opinions(TAX)

                if (FInfo.FullName.IndexOf(@"Tax Court Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objFOI = common_TAXCOURT_HeaderInfo(Content, OutputPath, CaseCounter.ToString(), FInfo.FullName);
                    if (objFOI.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objFOI.DecisionDate.Substring(objFOI.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objFOI.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objFOI.DecisionDate.Substring(objFOI.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objFOI.ShortName;
                    DocketNumber = objFOI.DocketNumber;
                    DecisionDate = objFOI.DecisionDate;
                    PartyHeader = objFOI.PartyHeader;
                    LibraryName = objFOI.LibraryName;
                    SavePath = objFOI.SaveFolderPath;
                    VolumeName = objFOI.VolumeName;
                }


                #endregion

                #region Superior Court Decisions

                if (FInfo.FullName.IndexOf(@"Superior Court Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCOURTDECISION = common_SUPCOURTDECISION_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCOURTDECISION.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCOURTDECISION.DecisionDate.Substring(objSUPCOURTDECISION.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCOURTDECISION.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCOURTDECISION.DecisionDate.Substring(objSUPCOURTDECISION.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCOURTDECISION.ShortName;
                    DocketNumber = objSUPCOURTDECISION.DocketNumber;
                    DecisionDate = objSUPCOURTDECISION.DecisionDate;
                    PartyHeader = objSUPCOURTDECISION.PartyHeader;
                    LibraryName = objSUPCOURTDECISION.LibraryName;
                    SavePath = objSUPCOURTDECISION.SaveFolderPath;
                    VolumeName = objSUPCOURTDECISION.VolumeName;
                }

                #endregion

                #region Commission on Human Rights

                if (FInfo.FullName.IndexOf(@"Commission on Human Rights", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objCHR = common_CHR_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objCHR.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objCHR.DecisionDate.Substring(objCHR.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objCHR.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objCHR.DecisionDate.Substring(objCHR.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objCHR.ShortName;
                    DocketNumber = objCHR.DocketNumber;
                    DecisionDate = objCHR.DecisionDate;
                    PartyHeader = objCHR.PartyHeader;
                    LibraryName = objCHR.LibraryName;
                    SavePath = objCHR.SaveFolderPath;
                    VolumeName = objCHR.VolumeName;
                }

                #endregion

                #region Labor Wage & Hour Decisions

                if (FInfo.FullName.IndexOf(@"Labor Wage & Hour Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objCHR = common_WCD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objCHR.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objCHR.DecisionDate.Substring(objCHR.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objCHR.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objCHR.DecisionDate.Substring(objCHR.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objCHR.ShortName;
                    DocketNumber = objCHR.DocketNumber;
                    DecisionDate = objCHR.DecisionDate;
                    PartyHeader = objCHR.PartyHeader;
                    LibraryName = objCHR.LibraryName;
                    SavePath = objCHR.SaveFolderPath;
                    VolumeName = objCHR.VolumeName;
                }

                #endregion


                #region Public Utility Decisions

                if (FInfo.FullName.IndexOf(@"Public Utility Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objPUC = common_PUC_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objPUC.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objPUC.DecisionDate.Substring(objPUC.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objPUC.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objPUC.DecisionDate.Substring(objPUC.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objPUC.ShortName;
                    DocketNumber = objPUC.DocketNumber;
                    DecisionDate = objPUC.DecisionDate;
                    PartyHeader = objPUC.PartyHeader;
                    LibraryName = objPUC.LibraryName;
                    SavePath = objPUC.SaveFolderPath;
                    VolumeName = objPUC.VolumeName;
                }

                #endregion

                #region Environmental Hearing Board

                if (FInfo.FullName.IndexOf(@"Environmental Hearing Board", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objEHB = common_EHB_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objEHB.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objEHB.DecisionDate.Substring(objEHB.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objEHB.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objEHB.DecisionDate.Substring(objEHB.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objEHB.ShortName;
                    DocketNumber = objEHB.DocketNumber;
                    DecisionDate = objEHB.DecisionDate;
                    PartyHeader = objEHB.PartyHeader;
                    LibraryName = objEHB.LibraryName;
                    SavePath = objEHB.SaveFolderPath;
                    VolumeName = objEHB.VolumeName;
                }
                #endregion

                #region Superior Court Opinions

                if (FInfo.FullName.IndexOf(@"Superior Court Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_SUPCO_HeaderInfo(Content, OutputPath, CaseCounter.ToString(), FInfo.FullName);
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }
                #endregion



                #region Education Board Orders

                if (FInfo.FullName.IndexOf(@"Education Board Orders", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_PEDBD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }


                #endregion

                #region Green Mountain Care Board Decisions

                if (FInfo.FullName.IndexOf(@"Green Mountain Care Board Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_GMCBD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }

                #endregion

                #region Human Services Board Decisions

                if (FInfo.FullName.IndexOf(@"Human Services Board Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_HSBD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }

                #endregion

                #region Professional Responsibility Board Decisions


                if (FInfo.FullName.IndexOf(@"Professional Responsibility Board Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_PCD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }

                #endregion

                #region Vermont Digest


                if (FInfo.FullName.IndexOf(@"Vermont Digest", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_VD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }


                #endregion


                #region Environmental Board Decisions


                if (FInfo.FullName.IndexOf(@"Environmental Board Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_ENVBD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }



                #endregion

                #region Growth Management Decisions



                if (FInfo.FullName.IndexOf(@"Growth Management Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_GMD_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }



                #endregion

                #region Public Utilities Commission

                if (FInfo.FullName.IndexOf(@"Public Utilities Commission", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objSUPCO = common_PUBUTL_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objSUPCO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objSUPCO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objSUPCO.DecisionDate.Substring(objSUPCO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objSUPCO.ShortName;
                    DocketNumber = objSUPCO.DocketNumber;
                    DecisionDate = objSUPCO.DecisionDate;
                    PartyHeader = objSUPCO.PartyHeader;
                    LibraryName = objSUPCO.LibraryName;
                    SavePath = objSUPCO.SaveFolderPath;
                    VolumeName = objSUPCO.VolumeName;
                }



                #endregion



                #region DSS Uniform Policy Manual
                if (FInfo.FullName.IndexOf(@"DSS Uniform Policy Manual", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {

                    HeaderOpinion objBARJ = common_DSSUPM_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objBARJ.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objBARJ.DecisionDate.Substring(objBARJ.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objBARJ.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objBARJ.DecisionDate.Substring(objBARJ.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objBARJ.ShortName;
                    DocketNumber = objBARJ.DocketNumber;
                    DecisionDate = objBARJ.DecisionDate;
                    PartyHeader = objBARJ.PartyHeader;
                    LibraryName = objBARJ.LibraryName;
                    SavePath = objBARJ.SaveFolderPath;
                    Browslevel1 = objBARJ.Browslevel1;
                    Browslevel2 = objBARJ.Browslevel2;
                    Description = objBARJ.Description;


                }





                #endregion


                #region Ethics Curbstone | Res Gestae

                if (FInfo.FullName.IndexOf(@"Ethics Curbstone  Res Gestae", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {

                    HeaderOpinion objBARJ = common_ETHICRG_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objBARJ.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objBARJ.DecisionDate.Substring(objBARJ.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objBARJ.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objBARJ.DecisionDate.Substring(objBARJ.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objBARJ.ShortName;
                    DocketNumber = objBARJ.DocketNumber;
                    DecisionDate = objBARJ.DecisionDate;
                    PartyHeader = objBARJ.PartyHeader;
                    LibraryName = objBARJ.LibraryName;
                    SavePath = objBARJ.SaveFolderPath;
                    Browslevel1 = objBARJ.Browslevel1;
                    Browslevel2 = objBARJ.Browslevel2;
                    Description = objBARJ.Description;


                }

                #endregion


                #region Res Gestae

                if (FInfo.FullName.IndexOf(@"Res Gestae2", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {

                    HeaderOpinion objBARJ = common_RG_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objBARJ.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objBARJ.DecisionDate.Substring(objBARJ.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objBARJ.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objBARJ.DecisionDate.Substring(objBARJ.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objBARJ.ShortName;
                    DocketNumber = objBARJ.DocketNumber;
                    DecisionDate = objBARJ.DecisionDate;
                    PartyHeader = objBARJ.PartyHeader;
                    LibraryName = objBARJ.LibraryName;
                    SavePath = objBARJ.SaveFolderPath;
                    Browslevel1 = objBARJ.Browslevel1;
                    Browslevel2 = objBARJ.Browslevel2;
                    Description = objBARJ.Description;


                }

                #endregion


                #region Common Function To Get EO Information From File


                if (FInfo.FullName.IndexOf(@"Ethics Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    if (FInfo.FullName.IndexOf(@"Ohio Ethics Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        OpinionHtml = Regex.Replace(OpinionHtml, "<div>", "<p>", RegexOptions.IgnoreCase);
                        OpinionHtml = Regex.Replace(OpinionHtml, "</div>", "</p>", RegexOptions.IgnoreCase);
                    }
                    HeaderOpinion objAGO = common_EO_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objAGO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objAGO.DecisionDate.Substring(objAGO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objAGO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objAGO.DecisionDate.Substring(objAGO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }

                    ShortName = objAGO.ShortName;
                    DocketNumber = objAGO.DocketNumber;
                    DecisionDate = objAGO.DecisionDate;
                    LibraryName = objAGO.LibraryName;
                    SavePath = objAGO.SaveFolderPath;
                }


                #endregion


                #region VT EO
                //if (FInfo.FullName.IndexOf(@"\vt\eo", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec");
                //    DocketNumber = ReadMetaTag(Content, "CodeSec");
                //    DecisionDate = ReadMetaTag(Content, "CodeTitle");
                //    DecisionDate = Convert.ToDateTime(DecisionDate + "/01/01").ToString("yyyy/MM/dd");
                //    LibraryName = "Vermont Advisory Ethics Opinions";
                //    /*CourtAbbreviation = "VT E.O.";*/
                //    SavePath = Path.Combine(OutputPath, "Vermont", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                //}
                #endregion

                #region OH EO
                //if (FInfo.FullName.IndexOf(@"Ohio Ethics Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    OpinionHtml = Regex.Replace(OpinionHtml, "<div>", "<p>", RegexOptions.IgnoreCase);
                //    OpinionHtml = Regex.Replace(OpinionHtml, "</div>", "</p>", RegexOptions.IgnoreCase);
                //    //OpinionHtml = Regex.Replace(OpinionHtml, @"</p>\s*<br\s*/>(\s|\n)*?<p>", "</p>" + Environment.NewLine + "<p>", RegexOptions.IgnoreCase);
                //    ShortName = ReadMetaTag(Content, "CodeSec");
                //    DocketNumber = ReadMetaTag(Content, "OpinionSearch");
                //    DecisionDate = ReadMetaTag(Content, "DateSearch");
                //    if (DecisionDate == "" || DecisionDate.Trim().Length == 4)
                //    {
                //        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    LibraryName = "Ohio Advisory Opinions";
                //    /*CourtAbbreviation = "VT E.O.";*/
                //    SavePath = Path.Combine(OutputPath, "Ohio", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                //}
                #endregion

                #region NH EO
                //if (FInfo.FullName.IndexOf(@"NH Ethics Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec");
                //    DocketNumber = ReadMetaTag(Content, "OpinionSearch");
                //    DecisionDate = ReadMetaTag(Content, "DateSearch");
                //    if (DecisionDate == "" || DecisionDate.Trim().Length == 4)
                //    {
                //        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    LibraryName = "NH Ethics Opinions";
                //    /*CourtAbbreviation = "VT E.O.";*/
                //    SavePath = Path.Combine(OutputPath, "New Hampshire", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                //}
                #endregion

                #region PA EO
                //if (FInfo.FullName.IndexOf(@"Pennsylvania State Ethics Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec");
                //    DocketNumber = ReadMetaTag(Content, "OpinionSearch");
                //    DecisionDate = ReadMetaTag(Content, "DateSearch");
                //    if (DecisionDate == "" || DecisionDate.Trim().Length == 4)
                //    {
                //        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    LibraryName = "Pennsylvania State Ethics Opinions";
                //    /*CourtAbbreviation = "VT E.O.";*/
                //    SavePath = Path.Combine(OutputPath, "Pennsylvania", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                //}
                #endregion


                #region KY EO
                //if (FInfo.FullName.IndexOf(@"\Kentucky\EO", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec");
                //    DocketNumber = ReadMetaTag(Content, "OpinionSearch");
                //    DecisionDate = ReadMetaTag(Content, "DateSearch");
                //    if (DecisionDate == "" || DecisionDate.Trim().Length == 4)
                //    {
                //        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    LibraryName = "Kentucky Ethics Opinions";
                //    /*CourtAbbreviation = "VT E.O.";*/
                //    SavePath = Path.Combine(OutputPath, "Kentucky", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                //}
                #endregion


                #region PA Pennsylvania Disciplinary Board Decisions
                else if (FInfo.FullName.IndexOf(@"\Pennsylvania Disciplinary Board Decisions\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    //ShortName = ReadMetaTag(Content, "ShortTitle").Trim();
                    ShortName = ReadMetaTag(Content, "title");
                    ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
                    ShortName = Regex.Replace(ShortName, @"\s\s(\s)*", " ").Trim();
                    DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();
                    DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
                    DateTime TDate;
                    if (!DateTime.TryParse(DecisionDate, out TDate))
                    {
                        DecisionDate = Convert.ToDateTime(DecisionDate + "/01/01").ToString("yyyy/MM/dd");
                    }
                    Author = ReadMetaTag(Content, "JudgePanel").Trim();
                    PartyHeader = ReadMetaTag(Content, "PartyName").Trim();
                    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                    LibraryName = "Pennsylvania Disciplinary Board Decisions";
                    SavePath = Path.Combine(OutputPath, "Pennsylvania", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                }
                #endregion


                #region NH NHPUBUTILITIES
                if (FInfo.FullName.IndexOf(@"NH Public Utility Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    ShortName = ReadMetaTag(Content, "title");
                    ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
                    DocketNumber = ReadMetaTag(Content, "DocketNo");
                    DecisionDate = ReadMetaTag(Content, "DateSearch");
                    if (DecisionDate == "" || DecisionDate.Trim() == "")
                    {
                        DecisionDate = ReadMetaTag(Content, "CaseDate2");
                    }
                    if (DecisionDate == "" || DecisionDate.Trim().Length == 4)
                    {
                        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                        DecisionDate = DecisionDate + "/01/01";
                    }

                    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                    LibraryName = "NH Public Utility Decisions";
                    /*CourtAbbreviation = "VT E.O.";*/
                    SavePath = Path.Combine(OutputPath, "New Hampshire", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                }
                #endregion

                #region NH Board of Tax and Land Appeals
                else if (FInfo.FullName.IndexOf(@"\NH Board of Tax and Land Appeals\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    //ShortName = ReadMetaTag(Content, "ShortTitle").Trim();
                    ShortName = ReadMetaTag(Content, "title");
                    ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
                    DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();
                    DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
                    DateTime TDate;
                    if (!DateTime.TryParse(DecisionDate, out TDate))
                    {
                        DecisionDate = Convert.ToDateTime(DecisionDate + "/01/01").ToString("yyyy/MM/dd");
                    }
                    Author = ReadMetaTag(Content, "JudgePanel").Trim();
                    PartyHeader = ReadMetaTag(Content, "PartyName").Trim();
                    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                    LibraryName = "NH Board of Tax and Land Appeals";
                    SavePath = Path.Combine(OutputPath, "New Hampshire", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                }
                #endregion

                #region VT Environment Court Decisions
                else if (FInfo.FullName.IndexOf(@"\vt\vtenvcrtd", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    string CodeTitle = ReadMetaTag(Content, "CodeTitle");
                    string CodeSec = ReadMetaTag(Content, "CodeSec");
                    string CatchLine = ReadMetaTag(Content, "Catchline");
                    string LogLine = CodeTitle + " " + CodeSec + " " + CatchLine;
                    ShortName = ReadMetaTag(Content, "Catchline");
                    DocketNumber = ReadMetaTag(Content, "CodeSec");
                    if (ShortName == "")
                    { ShortName = DocketNumber; }
                    DecisionDate = ReadMetaTag(Content, "DateSearch");
                    DecisionDate = DecisionDate.Replace("Filed Date: ", "").Replace("No response filed", "").Replace("Filed:", "").Trim();
                    if (DecisionDate == "")
                    {
                        DecisionDate = ReadMetaTag(Content, "CodeTitle");
                        DecisionDate = DecisionDate + "/01/01";
                    }
                    DateTime TDate;
                    if (!DateTime.TryParse(DecisionDate, out TDate))
                    {
                        DecisionDate = GetDecisionDate(DecisionDate);
                        if (!DateTime.TryParse(DecisionDate, out TDate))
                        {
                            string Log = File + "|" + LogLine + "|" + "Date not formatted correctly\r\n";
                            System.IO.File.AppendAllText(ErrorLogFile, Log);
                            continue;
                        }
                    }

                    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                    LibraryName = "Vermont Superior Court Environmental Division Decisions";
                    SavePath = Path.Combine(OutputPath, "Vermont", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                }
                #endregion

                #region VT Professional Responsibility
                else if (FInfo.FullName.IndexOf(@"\vt\vtpcd", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    string CodeTitle = ReadMetaTag(Content, "CodeTitle");
                    string CodeSec = ReadMetaTag(Content, "CodeSec");
                    string CatchLine = ReadMetaTag(Content, "Catchline");
                    string LogLine = CodeTitle + " " + CodeSec + " " + CatchLine;
                    ShortName = ReadMetaTag(Content, "CodeSec");
                    DocketNumber = ReadMetaTag(Content, "CodeSec");
                    DecisionDate = ReadMetaTag(Content, "DateSearch");
                    DecisionDate = DecisionDate.Replace("Filed Date: ", "").Replace("No response filed", "").Replace("Filed:", "").Trim();
                    if (DecisionDate == "")
                    {
                        DecisionDate = ReadMetaTag(Content, "CodeTitle");
                        DecisionDate = DecisionDate + "/01/01";
                    }
                    DateTime TDate;
                    if (!DateTime.TryParse(DecisionDate, out TDate))
                    {
                        DecisionDate = GetDecisionDate(DecisionDate);
                        if (!DateTime.TryParse(DecisionDate, out TDate))
                        {
                            string Log = File + "|" + LogLine + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                            System.IO.File.AppendAllText(ErrorLogFile, Log);
                            continue;
                        }
                    }
                    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                    LibraryName = "Vermont Professional Responsibility Board Decisions";
                    SavePath = Path.Combine(OutputPath, "Vermont", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                }
                #endregion

                #region VT Public Utility Commission
                else if (FInfo.FullName.IndexOf(@"\VT\VT Public Utility Commission\PSBD", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    //string CodeTitle = ReadMetaTag(Content, "CodeTitle");
                    //string CodeSec = ReadMetaTag(Content, "CodeSec");
                    //string CatchLine = ReadMetaTag(Content, "Catchline");
                    //string LogLine = CodeTitle + " " + CodeSec + " " + CatchLine;
                    ShortName = ReadMetaTag(Content, "ShortTitle").Trim();
                    DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();
                    DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
                    DateTime TDate;
                    if (!DateTime.TryParse(DecisionDate, out TDate))
                    {
                        DecisionDate = Convert.ToDateTime(DecisionDate + "/01/01").ToString("yyyy/MM/dd");
                    }
                    Author = ReadMetaTag(Content, "JudgePanel").Trim();
                    PartyHeader = ReadMetaTag(Content, "PartyName").Trim();
                    //DecisionDate = DecisionDate.Replace("Filed Date: ", "").Replace("No response filed", "").Replace("Filed:", "").Trim();
                    //if (DecisionDate == "")
                    //{
                    //    DecisionDate = ReadMetaTag(Content, "CodeTitle");
                    //    DecisionDate = DecisionDate + "/01/01";
                    //}
                    //DateTime TDate;
                    //if (!DateTime.TryParse(DecisionDate, out TDate))
                    //{
                    //    DecisionDate = GetDecisionDate(DecisionDate);
                    //    if (!DateTime.TryParse(DecisionDate, out TDate))
                    //    {
                    //        string Log = File + "|" + LogLine + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                    //        System.IO.File.AppendAllText(ErrorLogFile, Log);
                    //        continue;
                    //    }
                    //}
                    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                    LibraryName = "VT Public Utility Commission Decisions";
                    SavePath = Path.Combine(OutputPath, "Vermont", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                }
                #endregion

                #region VT Labour Relations Board
                else if (FInfo.FullName.IndexOf(@"\vt\vtlrbd", StringComparison.InvariantCultureIgnoreCase) >= 0 || FInfo.FullName.IndexOf(@"Vermont Labor Relations Board Decisions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    string CodeTitle = ReadMetaTag(Content, "CodeTitle");
                    string CodeSec = ReadMetaTag(Content, "CodeSec");
                    string LogLine = CodeTitle + " " + CodeSec;
                    ShortName = ReadMetaTag(Content, "Catchline");
                    DocketNumber = ReadMetaTag(Content, "Orig_CaseSearch");
                    DecisionDate = ReadMetaTag(Content, "DateSearch");
                    Author = ReadMetaTag(Content, "JudgeSearch");
                    PartyHeader = ReadMetaTag(Content, "Catchline");
                    DecisionDate = DecisionDate.Replace("Filed Date: ", "").Replace("No response filed", "").Replace("Filed:", "").Trim();
                    if (DecisionDate == "")
                    {
                        DecisionDate = ReadMetaTag(Content, "CodeTitle");
                        DecisionDate = DecisionDate + "/01/01";
                    }
                    DateTime TDate;
                    if (!DateTime.TryParse(DecisionDate, out TDate))
                    {
                        DecisionDate = GetDecisionDate(DecisionDate);
                        if (!DateTime.TryParse(DecisionDate, out TDate))
                        {
                            string Log = File + "|" + LogLine + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                            System.IO.File.AppendAllText(ErrorLogFile, Log);
                            continue;
                        }
                    }
                    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                    LibraryName = "Vermont Labor Relations Board Decisions";
                    SavePath = Path.Combine(OutputPath, "Vermont", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                }
                #endregion

                #region Human Service Board Decisions
                if (FInfo.FullName.IndexOf(@"\vt\case\hsbd", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    ShortName = ReadMetaTag(Content, "ShortTitle");
                    DocketNumber = ReadMetaTag(Content, "DocketNo");
                    DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
                    DateTime TDate;
                    if (!DateTime.TryParse(DecisionDate, out TDate))
                    {
                        DecisionDate = Convert.ToDateTime(DecisionDate + "/01/01").ToString("yyyy/MM/dd");
                        if (!DateTime.TryParse(DecisionDate, out TDate))
                        {
                            string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                            System.IO.File.AppendAllText(ErrorLogFile, Log);
                            continue;
                        }
                    }
                    PartyHeader = ReadMetaTag(Content, "PartyName");
                    LibraryName = "Vermont Human Services Board Decisions";
                    /*CourtAbbreviation = "VT E.O.";*/
                    SavePath = Path.Combine(OutputPath, "Vermont", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                }

                #endregion

                #region Common Function To Get BARJ Information From File 
                if (FInfo.FullName.IndexOf(@"Bar Journal", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {

                    HeaderOpinion objBARJ = common_BARJ_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objBARJ.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objBARJ.DecisionDate.Substring(objBARJ.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objBARJ.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objBARJ.DecisionDate.Substring(objBARJ.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objBARJ.ShortName;
                    DocketNumber = objBARJ.DocketNumber;
                    DecisionDate = objBARJ.DecisionDate;
                    PartyHeader = objBARJ.PartyHeader;
                    LibraryName = objBARJ.LibraryName;
                    SavePath = objBARJ.SaveFolderPath;
                    VolumeName = objBARJ.VolumeName;
                    Caption = objBARJ.Caption;
                    Description = objBARJ.Description;

                }
                //National Labor Relations Board

                if (FInfo.FullName.IndexOf(@"National Labor Relations Board", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {

                    HeaderOpinion objNLRB = common_NLRB_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objNLRB.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objNLRB.DecisionDate.Substring(objNLRB.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    bool flagValidateDate = validateDate(Convert.ToDateTime(objNLRB.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objNLRB.DecisionDate.Substring(objNLRB.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objNLRB.ShortName;
                    DocketNumber = objNLRB.DocketNumber;
                    DecisionDate = objNLRB.DecisionDate;
                    PartyHeader = objNLRB.PartyHeader;
                    LibraryName = objNLRB.LibraryName;
                    SavePath = objNLRB.SaveFolderPath;
                    VolumeName = objNLRB.VolumeName;
                    Caption = objNLRB.Caption;
                    Description = objNLRB.Description;

                }




                #endregion

               

                #region CO BARJ


                if (FInfo.FullName.IndexOf(@"\Colorado Lawyer\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    string ShortName1 = ReadMetaTag(Content, "DocketNo").Trim();
                    string ShortName2 = ReadMetaTag(Content, "Catchline").Trim();

                    if (ShortName1.IndexOf(",") > -1)
                    {
                        ShortName1 = ShortName1.Substring(0, ShortName1.IndexOf(","));
                    }
                    else if (ShortName1 != "")
                    {
                        string[] ArrayData = Regex.Split(ShortName1, "\\s", RegexOptions.IgnoreCase);

                        if (ArrayData.Count() > 2)
                        {
                            ShortName1 = ArrayData[0].ToString() + " " + ArrayData[1].ToString();
                        }
                        else
                        {
                            string abc = "";
                        }

                    }


                    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                    DocketNumber = ReadMetaTag(Content, "CodeSec").Trim();
                    DecisionDate = ReadMetaTag(Content, "CaseDate2").Trim();

                    if (DecisionDate == "")
                    {
                        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                        DecisionDate = DecisionDate + "/01/01";
                    }
                    if (Regex.IsMatch(ShortName, @"(Pg.)(\s|\n)*(\d|\w)+"))
                    {
                        string test = Regex.Match(ShortName, @"(Pg.)(\s|\n)*(\d|\w)+").Value;
                        string pagenumber = test.Replace("Pg.", "").Trim();
                        switch (pagenumber.Length)
                        {
                            case 1:
                                test = test.Replace(pagenumber, "000" + pagenumber);
                                break;
                            case 2:
                                test = test.Replace(pagenumber, "00" + pagenumber);
                                break;
                            case 3:
                                test = test.Replace(pagenumber, "0" + pagenumber);
                                break;
                        }
                        ShortName = ShortName.Replace(Regex.Match(ShortName, @"(Pg.)(\s|\n)*(\d|\w)+").Value, test);
                        if (Regex.IsMatch(ShortName, @"\d{4}(\s|\n)*(\,)*(\s|\n)*(January|February|March|April|May|June|July|August|September|October|November|December)"))
                        {
                            string YearStr = Regex.Match(ShortName, @"\d{4}(\s|\n)*(\,)*(\s|\n)*(January|February|March|April|May|June|July|August|September|October|November|December)").Value;
                            string Month = Regex.Match(YearStr, "(January|February|March|April|May|June|July|August|September|October|November|December)").Value;
                            string Year = Regex.Match(YearStr, @"\d{4}").Value;
                            switch (Month)
                            {
                                case "January":
                                    Month = "01";
                                    break;
                                case "February":
                                    Month = "02";
                                    break;
                                case "March":
                                    Month = "03";
                                    break;
                                case "April":
                                    Month = "04";
                                    break;
                                case "May":
                                    Month = "05";
                                    break;
                                case "June":
                                    Month = "06";
                                    break;
                                case "July":
                                    Month = "07";
                                    break;
                                case "August":
                                    Month = "08";
                                    break;
                                case "September":
                                    Month = "09";
                                    break;
                                case "October":
                                    Month = "10";
                                    break;
                                case "November":
                                    Month = "11";
                                    break;
                                case "December":
                                    Month = "12";
                                    break;
                            }
                            string newstr = Year + "-" + Month;
                            DateTime date = new DateTime(Convert.ToInt32(Year), Convert.ToInt32(Month), 1);
                            string getFullMonthName = date.ToString("MMMM");
                            ShortName = ShortName.Replace(YearStr, newstr);
                            ShortName = ShortName + " (" + getFullMonthName + ", " + Year + ")";
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }


                    if (ShortName1.Trim() == "")
                    {
                        ShortName = ShortName + ", " + ShortName2;
                    }
                    else
                    {

                        ShortName = ShortName1 + ", " + ShortName + ", " + ShortName2;
                    }



                    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                    PartyHeader = ReadMetaTag(Content, "Catchline").Trim();
                    LibraryName = "Colorado  Lawyer";
                    SavePath = Path.Combine(OutputPath, "Colorado", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                }

                #endregion


                #region Common Function To Get WCOMP Information From File 
                if (FInfo.FullName.IndexOf(@"Workers Compensation", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    HeaderOpinion objWCOMP = common_WCOMP_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objWCOMP.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objWCOMP.DecisionDate.Substring(objWCOMP.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }

                    bool flagValidateDate = validateDate(Convert.ToDateTime(objWCOMP.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objWCOMP.DecisionDate.Substring(objWCOMP.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objWCOMP.ShortName;
                    DocketNumber = objWCOMP.DocketNumber;
                    DecisionDate = objWCOMP.DecisionDate;
                    PartyHeader = objWCOMP.PartyHeader;
                    LibraryName = objWCOMP.LibraryName;
                    SavePath = objWCOMP.SaveFolderPath;
                }

                #endregion


                #region CO WCOMP

                //if (FInfo.FullName.IndexOf(@"\Colorado Workers Compensation Decisions\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();
                //    DecisionDate = ReadMetaTag(Content, "Casedate2").Trim();
                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    PartyHeader = ReadMetaTag(Content, "Catchline").Trim();
                //    LibraryName = "Colorado Workers' Compensation Decisions";
                //    SavePath = Path.Combine(OutputPath, "Colorado", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}
                #endregion

                #region NE WCOMP
                //if (FInfo.FullName.IndexOf(@"\Nebraska Workers Compensation Decisions\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "title").Trim();
                //    DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();
                //    DecisionDate = ReadMetaTag(Content, "DateSearch").Trim();

                //    if (DecisionDate.Length == 4)
                //    {
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    PartyHeader = ReadMetaTag(Content, "HeadingSearch").Trim();
                //    Author = ReadMetaTag(Content, "OpinionJudge");
                //    LibraryName = "Nebraska Workers Compensation Decisions";
                //    SavePath = Path.Combine(OutputPath, "Nebraska", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}
                #endregion

                #region MI WCOMP
                //if (FInfo.FullName.IndexOf(@"\Michigan Workers Compensation Opinions\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();
                //    DecisionDate = ReadMetaTag(Content, "DateSearch").Trim();

                //    if (DecisionDate.Length == 4)
                //    {
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    PartyHeader = ReadMetaTag(Content, "Catchline").Trim();
                //    Author = ReadMetaTag(Content, "OpinionJudge");
                //    LibraryName = "Michigan Workers' Compensation Opinions";
                //    SavePath = Path.Combine(OutputPath, "Michigan", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}
                #endregion

                #region VT WCOMP
                //if (FInfo.FullName.IndexOf(@"\Vermont Workers Compensation Decisions\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();

                //    DecisionDate = ReadMetaTag(Content, "DateSearch").Trim();

                //    if (DecisionDate.Length == 4)
                //    {
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    PartyHeader = ReadMetaTag(Content, "Catchline").Trim();
                //    Author = ReadMetaTag(Content, "OpinionJudge");
                //    LibraryName = "Vermont Workers' Compensation Decisions";
                //    SavePath = Path.Combine(OutputPath, "Vermont", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}
                #endregion

                #region NE AGO
                //if (FInfo.FullName.IndexOf(@"\Nebraska Attorney General Opinions\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "Search_Codesec").Trim();
                //    DecisionDate = ReadMetaTag(Content, "Casedate2").Trim();
                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    LibraryName = "Nebraska Attorney General Opinions";
                //    SavePath = Path.Combine(OutputPath, "Nebraska", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}

                #endregion

                #region NE LR No Needed
                //if (FInfo.FullName.IndexOf(@"\Nebraska Law Review\", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "CodeSec").Trim();
                //    PartyHeader = ReadMetaTag(Content, "Catchline");
                //    DecisionDate = ReadMetaTag(Content, "Casedate2").Trim();
                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    Author = ReadMetaTag(Content, "AuthorSearch");
                //    LibraryName = "Nebraska Law Review";
                //    SavePath = Path.Combine(OutputPath, "Nebraska", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}
                #endregion



                #region Common Function To Get AGO Information From File


                if (FInfo.FullName.IndexOf(@"Attorney General", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {

                    HeaderOpinion objAGO = common_AGO_HeaderInfo(Content, OutputPath, CaseCounter.ToString());
                    if (objAGO.DecisionDate.Trim().IndexOf("Casedate2|") > -1)
                    {
                        DecisionDate = objAGO.DecisionDate.Substring(objAGO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date not formatted correctly\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }

                    bool flagValidateDate = validateDate(Convert.ToDateTime(objAGO.DecisionDate.Trim()));
                    if (flagValidateDate == false)
                    {
                        DecisionDate = objAGO.DecisionDate.Substring(objAGO.DecisionDate.LastIndexOf("|") + 1);
                        string Log = File + "|" + "" + "|" + DecisionDate + "|" + "Date do not have proper value\r\n";
                        System.IO.File.AppendAllText(ErrorLogFile, Log);
                        continue;
                    }
                    ShortName = objAGO.ShortName;
                    DocketNumber = objAGO.DocketNumber;
                    DecisionDate = objAGO.DecisionDate;
                    LibraryName = objAGO.LibraryName;
                    SavePath = objAGO.SaveFolderPath;
                }


                #endregion


                #region ND AGO
                //if (FInfo.FullName.IndexOf(@"\North Dakota Attorney General Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "Search_Codesec").Trim();
                //    DecisionDate = ReadMetaTag(Content, "CaseDate2").Trim();

                //    if (DecisionDate == "")
                //    {
                //        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    LibraryName = "North Dakota Attorney General Opinions";
                //    SavePath = Path.Combine(OutputPath, "North Dakota", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}

                #endregion

                #region OH AGO
                //if (FInfo.FullName.IndexOf(@"\Ohio Attorney General Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "Search_Codesec").Trim();
                //    DecisionDate = ReadMetaTag(Content, "CaseDate2").Trim();

                //    if (DecisionDate == "")
                //    {
                //        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    LibraryName = "Ohio Attorney General Opinions";
                //    SavePath = Path.Combine(OutputPath, "Ohio", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}
                #endregion

                #region PA AGO
                //if (FInfo.FullName.IndexOf(@"\Pennsylvania Attorney General Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "Search_Codesec").Trim();
                //    DecisionDate = ReadMetaTag(Content, "CaseDate2").Trim();

                //    if (DecisionDate == "")
                //    {
                //        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    LibraryName = "Pennsylvania Attorney General Opinions";
                //    SavePath = Path.Combine(OutputPath, "Pennsylvania", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
                //}
                #endregion

                #region MI AGO
                //if (FInfo.FullName.IndexOf(@"\Michigan Attorney General Opinions", StringComparison.InvariantCultureIgnoreCase) >= 0)
                //{
                //    ShortName = ReadMetaTag(Content, "CodeSec").Trim();
                //    DocketNumber = ReadMetaTag(Content, "Search_Codesec").Trim();
                //    DecisionDate = ReadMetaTag(Content, "CaseDate2").Trim();

                //    if (DecisionDate == "")
                //    {
                //        DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                //        DecisionDate = DecisionDate + "/01/01";
                //    }

                //    DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
                //    Author = ReadMetaTag(Content, "PetitionerSearch");
                //    LibraryName = "Michigan Attorney General Opinions";
                //    SavePath = Path.Combine(OutputPath, "Michigan", LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

                //}
                #endregion


                if (HeaderHtml == "" || OpinionHtml == "")
                {
                    string Log = File + "|" + "Header or Opinion not found\r\n";
                    System.IO.File.AppendAllText(ErrorLogFile, Log);
                    //MoveSkipedFile(IncomingPath, File);
                    continue;
                }
                else
                {
                    if (HeaderHtml == "temporaryheader")
                    {
                        HeaderHtml = "";
                    }

                    XMLVersion.ShortName = ShortName;
                    if (DocketNumber != "")
                    {
                        XMLVersion.DocketNumbers = new List<Docket>();
                        if (DocketNumber.IndexOf(", ") > 0 && LibraryName != "Colorado Lawyer")
                        { DocketList = DocketNumber.Split(',').ToList(); }
                        else if (DocketNumber.IndexOf("; ") > 0)
                        { DocketList = DocketNumber.Split(';').ToList(); }
                        else
                        { DocketList.Add(DocketNumber); }
                        foreach (string DocketNo in DocketList)
                        {
                            if (DocketNo.Trim() != "")
                            { XMLVersion.DocketNumbers.Add(new Docket(DocketNo.Trim())); }
                        }
                        DocketList.Clear();
                        foreach (Docket DocketNo in XMLVersion.DocketNumbers)
                        {
                            DocketList.Add(DocketNo.DocketNumber);
                        }
                    }
                    XMLVersion.DecisionDate = DecisionDate;
                    XMLVersion.Author = Author;
                    XMLVersion.PartyHeader = PartyHeader;
                    XMLVersion.OpinionHtml = OpinionHtml;
                    XMLVersion.HeaderHtml = HeaderHtml;
                    XMLVersion.LibraryName = LibraryName;
                    XMLVersion.CourtAbbreviation = CourtAbbreviation;
                    XMLVersion.SaveFolderPath = SavePath;
                    XMLVersion.VolumeName = VolumeName;
                    XMLVersion.Caption = Caption;
                    XMLVersion.Description = Description;
                    XMLVersion.Browslevel1 = Browslevel1;
                    XMLVersion.Browslevel2 = Browslevel2;
                    XMLVersion.Browslevel3 = Browslevel3;

                    //--for normal conversion use this function

                    SaveOpinionInXml(XMLVersion, File, IncomingPath, OutputPath, ErrorLogFile);


                    //---use below code when need to create hierarchy for fastcase 

                    //  SaveOpinionInXml_New(XMLVersion, File, IncomingPath, OutputPath, ErrorLogFile);

                    //-----------------End--------------------

                    //MoveConvertedFile(IncomingPath, File);

                    //AllMetaValue = Get_AllMetaValue(Content, XMLVersion, File, IncomingPath, OutputPath, ErrorLogFile);
                    //AllHTMLData.Add(AllMetaValue);

                    CaseCounter += 1;
                }


            }
            if (global_Session._global_instance.savepath != null)
            {
                global_Session._global_instance.xmlDocument.Save(global_Session._global_instance.savepath);

            }



            //if (AllHTMLData != null)
            //{
            //    // AllHTMLData = AllHTMLData.OrderBy(O => O.CodeTitle).ToList();
            //    AllHTMLData = AllHTMLData.OrderByDescending(O => O.CodeTitle).ToList();
            //    //  AllHTMLData = AllHTMLData.OrderByDescending(O => Convert.ToInt32(O.SortOrderValue)).ToList();
            //    foreach (html_meta_model ObjItem in AllHTMLData)
            //    {
            //        SaveOpinionInXml_New2(ObjItem.OpinionData, ObjItem.File, ObjItem.IncomingPath, ObjItem.OutputPath, ObjItem.ErrorLogFile);

            //    }
            //}

        }


        static string GetDecisionDate(string DecisionDate)
        {
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                string MonthsList = @"(January|Jan(\.)?|February|Feb(\.)?|March|Mar(\.)?|April|Apr(\.)?|May|June|Jun|July|Jul|August|Aug(\.)?|September|Sept(\.)?|October|Oct(\.)?|November|Nov(\.)?|December|Dec(\.)?)";
                Regex DateReg = new Regex(@"(\d{1,2}(st|nd|rd|th)?\s*(day)?\s*of\s+" + MonthsList + @"\,*\s+\d{4})", RegexOptions.IgnoreCase);
                Regex DateReg2 = new Regex(MonthsList + @"\s+\d{1,2}(st|nd|rd|th)?\s*\,+\s+\d{4}", RegexOptions.IgnoreCase);
                Regex DateReg3 = new Regex(@"((_)+\s*(st|nd|rd|th)?\s*day\s+of\s+" + MonthsList + @"\,*\s+\d{4})", RegexOptions.IgnoreCase);
                Regex DateReg4 = new Regex(@"\d{1,2}/\d{1,2}/\d{1,4}", RegexOptions.IgnoreCase);
                Regex DateReg5 = new Regex(MonthsList + @"\s+(_)+\s*\,+\s+\d{4}", RegexOptions.IgnoreCase);
                Regex DateReg6 = new Regex(@"(\d{1,2}(-|\s)+" + MonthsList + @"(-|\s|\,)+\d{4})", RegexOptions.IgnoreCase);
                if (DateReg.IsMatch(DecisionDate))
                {
                    Match MatchDate = DateReg.Match(DecisionDate);
                    //DecisionDate = MatchDate.Groups[1].Value + " " + MatchDate.Groups[0].Value + ", " + MatchDate.Groups[2].Value;
                    string Day = Regex.Match(DecisionDate, @"\d{1,2}(st|nd|rd|th)?", RegexOptions.IgnoreCase).Value.Replace("st", "").Replace("nd", "").Replace("rd", "").Replace("th", "");
                    string Month = Regex.Match(DecisionDate, MonthsList, RegexOptions.IgnoreCase).Value;
                    string Year = Regex.Match(DecisionDate, @"\d{4}", RegexOptions.IgnoreCase).Value;
                    DecisionDate = Month + " " + Day + ", " + Year;
                }
                else if (DateReg2.IsMatch(DecisionDate))
                {
                    Match MatchDate = DateReg2.Match(DecisionDate);
                    DecisionDate = MatchDate.Value.Replace("st,", ",").Replace("nd,", ",").Replace("rd,", ",").Replace("th,", ",").Replace(" ,", ",");
                }
                else if (DateReg3.IsMatch(DecisionDate))
                {
                    Match MatchDate = DateReg3.Match(DecisionDate);
                    //DecisionDate = MatchDate.Groups[1].Value + " " + MatchDate.Groups[0].Value + ", " + MatchDate.Groups[2].Value;
                    string Day = "01";
                    string Month = Regex.Match(DecisionDate, MonthsList, RegexOptions.IgnoreCase).Value;
                    string Year = Regex.Match(DecisionDate, @"\d{4}", RegexOptions.IgnoreCase).Value;
                    DecisionDate = Month + " " + Day + ", " + Year;
                }
                else if (DateReg4.IsMatch(DecisionDate))
                {
                    Match MatchDate = DateReg4.Match(DecisionDate);
                    DecisionDate = MatchDate.Value;
                }
                else if (DateReg5.IsMatch(DecisionDate))
                {
                    string Month = Regex.Match(DecisionDate, MonthsList, RegexOptions.IgnoreCase).Value;
                    string Year = Regex.Match(DecisionDate, @"\d{4}", RegexOptions.IgnoreCase).Value;
                    DecisionDate = Month + " 01, " + Year;
                }
                else if (DateReg6.IsMatch(DecisionDate))
                {
                    Match MatchDate = DateReg6.Match(DecisionDate);
                    //DecisionDate = MatchDate.Groups[1].Value + " " + MatchDate.Groups[0].Value + ", " + MatchDate.Groups[2].Value;
                    string Day = Regex.Match(DecisionDate, @"\d{1,2}", RegexOptions.IgnoreCase).Value.Replace("st", "").Replace("nd", "").Replace("rd", "").Replace("th", "");
                    string Month = Regex.Match(DecisionDate, MonthsList, RegexOptions.IgnoreCase).Value;
                    string Year = Regex.Match(DecisionDate, @"\d{4}", RegexOptions.IgnoreCase).Value;
                    DecisionDate = Month + " " + Day + ", " + Year;
                }
                else if (DecisionDate.Length == 4 && DateTime.TryParse("01/01/" + DecisionDate, out TDate))
                {
                    DecisionDate = Convert.ToDateTime("01/01/" + DecisionDate).ToString("yyyy/MM/dd");
                }
                else if (Regex.IsMatch(DecisionDate, @"\d{4}\\\d{2}\\\d{2}", RegexOptions.IgnoreCase) == true)
                {
                    DecisionDate = DecisionDate.Replace("\\", "/");
                }
            }
            return DecisionDate;
        }
        static void SaveOpinionInXml(Opinion XMLVersion, string FilePath, string IncomingPath, string OutgoingPath, string ErrorLogFile)
        {
            string FullFilePath = XMLVersion.SaveFolderPath;
            FileInfo FInfo = new FileInfo(FullFilePath);
            string FileName = FInfo.Name;
            string FinalPath = FInfo.Directory.FullName;
            string ProductionPath = string.Empty;
            Directory.CreateDirectory(FinalPath);
            using (var stringwriter = new Utf8StringWriter())
            {
                var serializer = new XmlSerializer(XMLVersion.GetType());
                serializer.Serialize(stringwriter, XMLVersion);
                string XMLCase = stringwriter.ToString();
                XmlDocument XDoc = new XmlDocument();
                XDoc.LoadXml(XMLCase);
                XmlNode CaseNode = XDoc.SelectSingleNode(".//Opinion");
                CaseNode.Attributes.RemoveAll();
                XDoc.DocumentElement.RemoveChild(XDoc.SelectSingleNode(".//SaveFolderPath"));
                if (XDoc.SelectSingleNode(".//ProductionFolderPath") != null)
                { XDoc.DocumentElement.RemoveChild(XDoc.SelectSingleNode(".//ProductionFolderPath")); }

                if (XDoc.SelectSingleNode(".//ShortName") != null)
                {
                    if (string.IsNullOrEmpty(XDoc.SelectSingleNode(".//ShortName").InnerText))
                    { XDoc.DocumentElement.RemoveChild(XDoc.SelectSingleNode(".//ShortName")); }
                }
                if (XDoc.SelectSingleNode(".//DecisionDate") != null)
                {
                    if (string.IsNullOrEmpty(XDoc.SelectSingleNode(".//DecisionDate").InnerText))
                    { XDoc.DocumentElement.RemoveChild(XDoc.SelectSingleNode(".//DecisionDate")); }
                }
                if (XDoc.SelectSingleNode(".//Author") != null)
                {
                    if (string.IsNullOrEmpty(XDoc.SelectSingleNode(".//Author").InnerText))
                    { XDoc.DocumentElement.RemoveChild(XDoc.SelectSingleNode(".//Author")); }
                }
                if (XDoc.SelectSingleNode(".//PartyHeader") != null)
                {
                    if (string.IsNullOrEmpty(XDoc.SelectSingleNode(".//PartyHeader").InnerText))
                    { XDoc.DocumentElement.RemoveChild(XDoc.SelectSingleNode(".//PartyHeader")); }
                }
                if (XDoc.SelectSingleNode(".//CourtAbbreviation") != null)
                {
                    if (string.IsNullOrEmpty(XDoc.SelectSingleNode(".//CourtAbbreviation").InnerText))
                    { XDoc.DocumentElement.RemoveChild(XDoc.SelectSingleNode(".//CourtAbbreviation")); }
                }

                if (XDoc.SelectSingleNode(".//Docket") != null)
                {
                    foreach (XmlNode XDocket in XDoc.SelectNodes(".//Docket"))
                    {
                        XmlNode XD = XDocket.SelectSingleNode("DocketNumber");
                        XDocket.ParentNode.AppendChild(XD);
                    }
                    do
                    {
                        XDoc.SelectSingleNode(".//DocketNumbers").RemoveChild(XDoc.SelectSingleNode(".//Docket"));
                    } while (XDoc.SelectSingleNode(".//Docket") != null);
                }
                XMLCase = XDoc.OuterXml;
                MemoryStream mStream = new MemoryStream();
                XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                XDoc.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                mStream.Position = 0;
                StreamReader sReader = new StreamReader(mStream);
                XMLCase = sReader.ReadToEnd();
                mStream.Close();
                writer.Close();

                File.WriteAllText(FinalPath + "\\" + FileName, XMLCase, Encoding.UTF8);
                Console.WriteLine("Successfull (" + FileName + ")");
            }
        }
        static void MoveConvertedFile(string IncomingPath, string File)
        {
            string Destination = IncomingPath + @"Converted\";
            string FileName = File.Substring(File.LastIndexOf("\\") + 1);
            Destination = Destination + "\\" + Regex.Replace(File, IncomingPath.Replace("\\", "\\\\"), "", RegexOptions.IgnoreCase);
            Destination = Destination.Replace(FileName, "");
            Directory.CreateDirectory(Destination);
            System.IO.File.Move(File, Destination + @"\" + FileName);
        }
        static void MoveSkipedFile(string IncomingPath, string File)
        {
            string Destination = IncomingPath + @"ConversionError\";
            string FileName = File.Substring(File.LastIndexOf("\\") + 1);
            Destination = Destination + "\\" + Regex.Replace(File, IncomingPath.Replace("\\", "\\\\"), "", RegexOptions.IgnoreCase);
            Destination = Destination.Replace(FileName, "");
            Directory.CreateDirectory(Destination);
            System.IO.File.Move(File, Destination + @"\" + FileName);
        }
        static string GetOpinion(string Content)
        {
            string Opinion = string.Empty;
            try
            {
                int HeaderStart = Content.IndexOf("<div id=\"nllheader\">");
                if (HeaderStart >= 0)
                {
                    int HeaderEnd = Content.IndexOf("</div>", StringComparison.InvariantCultureIgnoreCase);
                    if (HeaderEnd >= 0)
                    {
                        /*Check if there are <nllcatch> or <nllcodesect> tags in the opinion. 
                         * If there are start the opinion after that*/
                        int NllCatchStart = Content.IndexOf("<nllcatch>", StringComparison.InvariantCultureIgnoreCase);
                        if (NllCatchStart >= 0)
                        { HeaderEnd = Content.IndexOf("<p", NllCatchStart, StringComparison.InvariantCultureIgnoreCase); }
                        else
                        {
                            int NllCodeSectStart = Content.IndexOf("<nllcodesect>", StringComparison.InvariantCultureIgnoreCase);
                            if (NllCodeSectStart >= 0)
                            { HeaderEnd = Content.IndexOf("<p", NllCodeSectStart, StringComparison.InvariantCultureIgnoreCase); }
                            else
                            { HeaderEnd += 6; }

                        }
                        Opinion = Content.Substring(HeaderEnd);
                    }
                }
                if (HeaderStart == -1)
                {
                    int HeaderEnd = Content.IndexOf("<!--OPINION TEXT STARTS HERE//-->");
                    if (HeaderEnd >= 0)
                    {
                        Opinion = Content.Substring(HeaderEnd + "<!--OPINION TEXT STARTS HERE//-->".Length);
                    }

                    if (Opinion.Trim() == "")
                    {
                        HeaderEnd = Content.IndexOf("<body>");
                        if (HeaderEnd >= 0)
                        {
                            Opinion = Content.Substring(HeaderEnd + "<body>".Length);
                        }
                    }
                }



            }
            catch (Exception)
            {

            }
            return Opinion;
        }
        static string GetHeader(string Content)
        {
            string Header = string.Empty;
            try
            {
                int HeaderStart = Content.IndexOf("<div id=\"nllheader\">");
                if (HeaderStart >= 0)
                {
                    int HeaderEnd = Content.IndexOf("</div>", HeaderStart);
                    Header = Content.Substring(HeaderStart, HeaderEnd - HeaderStart);
                    Header = Regex.Replace(Header, "<p>", "<p><center>", RegexOptions.IgnoreCase);
                    Header = Regex.Replace(Header, "</p>", "</center></p>", RegexOptions.IgnoreCase);

                    int CodeSecStart = Content.IndexOf("<nllcodesect>", StringComparison.InvariantCultureIgnoreCase);
                    if (CodeSecStart >= 0)
                    {
                        int CodeSecEnd = Content.IndexOf("</nllcodesect>", CodeSecStart, StringComparison.InvariantCultureIgnoreCase);
                        string CodeSec = Content.Substring(CodeSecStart, CodeSecEnd + "</nllcodesect>".Length - CodeSecStart);
                        Header += "\r\n" + CodeSec;
                        Header = Regex.Replace(Header, "<nllcodesect>", "<nllcodesect><p><center>", RegexOptions.IgnoreCase);
                        Header = Regex.Replace(Header, "</nllcodesect>", "</center></p></nllcodesect>", RegexOptions.IgnoreCase);
                    }
                    int CatchStart = Content.IndexOf("<nllcatch>", StringComparison.InvariantCultureIgnoreCase);
                    if (CatchStart >= 0)
                    {
                        int CatchEnd = Content.IndexOf("</nllcatch>", CatchStart, StringComparison.InvariantCultureIgnoreCase);
                        string Catch = Content.Substring(CatchStart, CatchEnd + "</nllcatch>".Length - CatchStart);
                        Header += "\r\n" + Catch;
                        Header = Regex.Replace(Header, "<nllcatch>", "<nllcatch><p><center>", RegexOptions.IgnoreCase);
                        Header = Regex.Replace(Header, "</nllcatch>", "</center></p></nllcatch>", RegexOptions.IgnoreCase);
                    }
                }
                if (HeaderStart == -1)
                {
                    HeaderStart = Content.IndexOf("<p>", StringComparison.InvariantCultureIgnoreCase);
                    if (HeaderStart >= 0)
                    {
                        int HeaderEnd = Content.IndexOf("<!--OPINION TEXT STARTS HERE//-->");
                        if (HeaderEnd >= 0)
                        {
                            Header = Content.Substring(HeaderStart, HeaderEnd - HeaderStart);
                        }

                    }
                }
            }
            catch (Exception)
            {

            }
            return Header;
        }
        static string ReadMetaTag(string Content, string MetaName)
        {
            string MetaValue = string.Empty;
            int MetaEnd = 0;
            string MetaTag = "<META NAME=\"" + MetaName + "\" CONTENT=\"";
            int MetaSt = Content.IndexOf(MetaTag, StringComparison.InvariantCultureIgnoreCase);
            if (MetaSt == -1)
            {
                MetaTag = "<META NAME=\"" + MetaName + "\" CONTENT=";
                MetaSt = Content.IndexOf(MetaTag, StringComparison.InvariantCultureIgnoreCase);
            }
            if (MetaSt > 0)
            {
                MetaEnd = Content.IndexOf(">", MetaSt);
                if (MetaEnd > MetaSt)
                {
                    MetaEnd += 1;
                    string CompleteMetaTag = Content.Substring(MetaSt, MetaEnd - MetaSt);
                    MetaValue = Regex.Replace(CompleteMetaTag, MetaTag, "", RegexOptions.IgnoreCase);
                    MetaValue = MetaValue.Replace("\"", "").Replace("/>", "").Replace(">", "");
                    MetaValue = Regex.Replace(MetaValue, "\r\n ", "").Trim();
                    MetaValue = Regex.Replace(MetaValue, @"\s\s(\s)*", " ").Trim();
                }
            }
            else if (MetaName.ToUpper() == "TITLE")
            {
                MetaSt = Content.IndexOf("<title>", StringComparison.InvariantCultureIgnoreCase);
                if (MetaSt > 0)
                {
                    MetaEnd = Content.IndexOf("</title>", MetaSt, StringComparison.InvariantCultureIgnoreCase);
                    if (MetaEnd > MetaSt)
                    {
                        string CompleteMetaTag = Content.Substring(MetaSt, MetaEnd - MetaSt);
                        MetaValue = Regex.Replace(CompleteMetaTag, "<title>", "", RegexOptions.IgnoreCase);
                    }
                }
            }
            return MetaValue;
        }
        static List<string> GetAllFile(string IncomingPath)
        {
            List<string> FilesList = new List<string>();
            List<string> DirectoryList = Directory.GetDirectories(IncomingPath).ToList();
            foreach (string Folder in DirectoryList)
            {
                if (Folder.IndexOf(@"\Converted\", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                   Folder.IndexOf(@"\Skipped\", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                   Folder.IndexOf(@"\ConversionError\", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                   Folder.IndexOf(@"\Deleted\", StringComparison.InvariantCultureIgnoreCase) == -1)
                { FilesList.AddRange(GetInnerFiles(Folder)); }
            }
            return FilesList;
        }


        static List<string> GetInnerFiles(string ParentFolder)
        {
            List<string> FilesList = new List<string>();
            if (ParentFolder.IndexOf(@"\Converted\") == -1 &&
                ParentFolder.IndexOf(@"\Skipped\") == -1 &&
                ParentFolder.IndexOf(@"\ConversionError\") == -1 &&
                ParentFolder.IndexOf(@"\Deleted\") == -1)
            {
                if (Directory.GetFiles(ParentFolder).Length > 0)
                {
                    FilesList.AddRange(Directory.GetFiles(ParentFolder, "*.htm*").ToList());
                }
                if (Directory.GetDirectories(ParentFolder).Length > 0)
                {
                    List<string> DirectoryList = Directory.GetDirectories(ParentFolder).ToList();
                    foreach (string Folder in DirectoryList)
                    {
                        FilesList.AddRange(GetInnerFiles(Folder));
                    }
                }
            }
            return FilesList;
        }

        //created by santosh


        static List<string> GetAllFile_2(string IncomingPath)
        {
            List<string> FilesList = new List<string>();
            List<string> DirectoryList = Directory.GetDirectories(IncomingPath).ToList();
            foreach (string Folder in DirectoryList)
            {
                if (Folder.IndexOf(@"\Converted\", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                   Folder.IndexOf(@"\Skipped\", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                   Folder.IndexOf(@"\ConversionError\", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                   Folder.IndexOf(@"\Deleted\", StringComparison.InvariantCultureIgnoreCase) == -1)
                { FilesList.AddRange(GetInnerFiles_2(Folder)); }
            }
            return FilesList;
        }
        static List<string> GetInnerFiles_2(string ParentFolder)
        {
            List<string> FilesList = new List<string>();
            if (ParentFolder.IndexOf(@"\Converted\") == -1 &&
                ParentFolder.IndexOf(@"\Skipped\") == -1 &&
                ParentFolder.IndexOf(@"\ConversionError\") == -1 &&
                ParentFolder.IndexOf(@"\Deleted\") == -1)
            {
                if (Directory.GetFiles(ParentFolder).Length > 0)
                {
                    FilesList.AddRange(Directory.GetFiles(ParentFolder, "*.xml*").ToList());
                }
                if (Directory.GetDirectories(ParentFolder).Length > 0)
                {
                    List<string> DirectoryList = Directory.GetDirectories(ParentFolder).ToList();
                    foreach (string Folder in DirectoryList)
                    {
                        FilesList.AddRange(GetInnerFiles_2(Folder));
                    }
                }
            }
            return FilesList;
        }


        static void SaveOpinionInXml_New(Opinion XMLVersion, string FilePath, string IncomingPath, string OutgoingPath, string ErrorLogFile)
        {
            string FullFilePath = XMLVersion.SaveFolderPath;
            FileInfo FInfo = new FileInfo(FullFilePath);
            string FileName = FInfo.Name;
            string FinalPath = FInfo.Directory.FullName;
            string VolumeName = XMLVersion.VolumeName;
            if (VolumeName != null)
            {
                FinalPath = FinalPath + "\\" + VolumeName;
                string ProductionPath = string.Empty;

                if (!Directory.Exists(FinalPath))
                {
                    Directory.CreateDirectory(FinalPath);
                    string SaveXmlPath = FinalPath + "\\" + FileName;
                    secondary_xml_data_model objData = new secondary_xml_data_model();

                    objData.level_of_data = "3";

                    index_element_data_model objLevel1 = new index_element_data_model();
                    objLevel1.caption = XMLVersion.Browslevel2;
                    objLevel1.description = "";
                    objLevel1.sortorder = "1";
                    objLevel1.level = "1";
                    objLevel1.haschildlevel = "1";
                    objData.level1 = objLevel1;



                    index_element_data_model objLevel2 = new index_element_data_model();
                    objLevel2.caption = XMLVersion.Browslevel3;
                    objLevel2.description = "";
                    objLevel2.sortorder = "1";
                    objLevel2.level = "2";
                    objLevel2.haschildlevel = "1";
                    objData.level2 = objLevel2;

                    //index_element_data_model objLevel3 = new index_element_data_model();
                    //objLevel3.caption = XMLVersion.Browslevel3;
                    //objLevel3.description = "";
                    //objLevel3.sortorder = "1";
                    //objLevel3.level = "3";
                    //objLevel3.haschildlevel = "1";
                    //objData.level3 = objLevel3;


                    index_element_data_model objLevelLastNode = new index_element_data_model();
                    objLevelLastNode.caption = "";
                    objLevelLastNode.description = XMLVersion.Caption + XMLVersion.Description;
                    objLevelLastNode.sortorder = "1";
                    objLevelLastNode.shortname = XMLVersion.PartyHeader;
                    objLevelLastNode.revisionhistory = "";
                    objLevelLastNode.haschildlevel = "0";
                    objLevelLastNode.content = XMLVersion.HeaderHtml + "\n" + XMLVersion.OpinionHtml;
                    objLevelLastNode.level = "3";

                    objData.level3 = objLevelLastNode;


                    objData.edition_date = "01/01/"+ XMLVersion.Browslevel1 + "";
                    objData.revision_date = "";
                    objData.currency_text = ""+ XMLVersion.Browslevel1 + "";
                    objData.library_edition_description = "Federal Register (" + XMLVersion.Browslevel1 + " Ed.)";
                    objData.library_SourceConst = "FEDREGULATIONS";
                    objData.library_name = XMLVersion.LibraryName;

                    if (global_Session._global_instance.savepath != null)
                    {
                        if (SaveXmlPath != global_Session._global_instance.savepath)
                        {
                            global_Session._global_instance.xmlDocument.Save(global_Session._global_instance.savepath);
                        }
                    }
                    global_Session._global_instance.BrowseLevel1 = XMLVersion.Browslevel1;
                    global_Session._global_instance.BrowseLevel2 = XMLVersion.Browslevel2;
                    global_Session._global_instance.BrowseLevel3 = XMLVersion.Browslevel3;
                    global_Session._global_instance.BrowseLevel4 = XMLVersion.Browslevel4;
                    global_Session._global_instance.savepath = SaveXmlPath;
                    CreateXml(objData, SaveXmlPath);

                }
                else
                {

                    string[] AllXmlFile = Directory.GetFiles(FinalPath, "*.xml", SearchOption.TopDirectoryOnly);
                    if (AllXmlFile.Count() > 0)
                    {
                        string SaveXmlPath = AllXmlFile[0].ToString();
                        if (global_Session._global_instance.BrowseLevel3 == XMLVersion.Browslevel3 &&
                            global_Session._global_instance.BrowseLevel2 == XMLVersion.Browslevel2 &&
                            global_Session._global_instance.BrowseLevel1 == XMLVersion.Browslevel1)
                        {
                            index_element_data_model objLevel = new index_element_data_model();
                            objLevel.caption = "";
                            objLevel.description = XMLVersion.Caption + XMLVersion.Description;
                            objLevel.sortorder = "";
                            objLevel.shortname = XMLVersion.PartyHeader;
                            objLevel.revisionhistory = "";
                            objLevel.haschildlevel = "0";
                            objLevel.content = XMLVersion.HeaderHtml + "\n" + XMLVersion.OpinionHtml;
                            objLevel.level = "3";
                            UpdateXml(SaveXmlPath, objLevel, XMLVersion.Browslevel3);
                        }
                        else if (global_Session._global_instance.BrowseLevel2 == XMLVersion.Browslevel2 &&
                            global_Session._global_instance.BrowseLevel1 == XMLVersion.Browslevel1)
                        {
                            secondary_xml_data_model objData = new secondary_xml_data_model();
                            objData.level_of_data = "2";


                            XmlDocument doc = new XmlDocument();
                            if (global_Session._global_instance.xmlDocument != null)
                            {
                                doc = global_Session._global_instance.xmlDocument;
                            }
                            XmlElement oldNode1 = null;
                            string CaptionValue = XMLVersion.Browslevel2;
                            oldNode1 = (XmlElement)doc.SelectSingleNode("//Index[@Level='1'][Caption='" + CaptionValue + "']//Indexes").LastChild;
                            int orderNumber = 1;

                            if (oldNode1.SelectSingleNode(".//SortOrder") != null)
                            {
                                orderNumber = Convert.ToInt32(oldNode1.SelectSingleNode(".//SortOrder").InnerText) + 1;
                            }

                            index_element_data_model objLevel3 = new index_element_data_model();
                            objLevel3.caption = XMLVersion.Browslevel3;
                            objLevel3.description = "";
                            objLevel3.sortorder = Convert.ToString(orderNumber);
                            objLevel3.level = "2";
                            objLevel3.haschildlevel = "1";
                            objData.level1 = objLevel3;

                            index_element_data_model objLevelLastNode = new index_element_data_model();
                            objLevelLastNode.caption = "";
                            objLevelLastNode.description = XMLVersion.Caption + XMLVersion.Description;
                            objLevelLastNode.sortorder = "1";
                            objLevelLastNode.shortname = XMLVersion.PartyHeader;
                            objLevelLastNode.revisionhistory = "";
                            objLevelLastNode.haschildlevel = "0";
                            objLevelLastNode.content = XMLVersion.HeaderHtml + "\n" + XMLVersion.OpinionHtml;
                            objLevelLastNode.level = "3";

                            objData.level2 = objLevelLastNode;

                            XmlElement SecondNewNode = CreateXmlNewNode(objData);
                            UpdateXmlNewNode(SecondNewNode, "2", XMLVersion.Browslevel2);
                            global_Session._global_instance.BrowseLevel3 = XMLVersion.Browslevel3;
                            //global_Session._global_instance.xmlDocument.Save(global_Session._global_instance.savepath);
                        }
                        else if (global_Session._global_instance.BrowseLevel1 == XMLVersion.Browslevel1)
                        {
                            secondary_xml_data_model objData = new secondary_xml_data_model();
                            objData.level_of_data = "3";


                            XmlDocument doc = new XmlDocument();
                            if (global_Session._global_instance.xmlDocument != null)
                            {
                                doc = global_Session._global_instance.xmlDocument;
                            }
                            XmlElement oldNode1 = null;

                            string CaptionValue = XMLVersion.Browslevel2;
                            //oldNode1 = (XmlElement)doc.SelectSingleNode("//Index[@Level='1'][Caption='" + CaptionValue + "']//Indexes").LastChild;
                            oldNode1 = (XmlElement)doc.SelectSingleNode("//Indexes").LastChild;
                            int orderNumber = 1;

                            if (oldNode1.SelectSingleNode(".//SortOrder") != null)
                            {
                                orderNumber = Convert.ToInt32(oldNode1.SelectSingleNode(".//SortOrder").InnerText) + 1;
                            }

                            index_element_data_model objLevel1 = new index_element_data_model();
                            objLevel1.caption = XMLVersion.Browslevel2;
                            objLevel1.description = "";
                            objLevel1.sortorder = Convert.ToString(orderNumber);
                            objLevel1.level = "1";
                            objLevel1.haschildlevel = "1";
                            objData.level1 = objLevel1;

                            index_element_data_model objLevel2 = new index_element_data_model();
                            objLevel2.caption = XMLVersion.Browslevel3;
                            objLevel2.description = "";
                            objLevel2.sortorder = Convert.ToString(orderNumber);
                            objLevel2.level = "2";
                            objLevel2.haschildlevel = "1";
                            objData.level2 = objLevel2;

                            index_element_data_model objLevelLastNode = new index_element_data_model();
                            objLevelLastNode.caption = "";
                            objLevelLastNode.description = XMLVersion.Caption + XMLVersion.Description;
                            objLevelLastNode.sortorder = "1";
                            objLevelLastNode.shortname = XMLVersion.PartyHeader;
                            objLevelLastNode.revisionhistory = "";
                            objLevelLastNode.haschildlevel = "0";
                            objLevelLastNode.content = XMLVersion.HeaderHtml + "\n" + XMLVersion.OpinionHtml;
                            objLevelLastNode.level = "3";

                            objData.level3 = objLevelLastNode;

                            XmlElement SecondNewNode = CreateXmlNewNode(objData);
                            UpdateXmlNewNode(SecondNewNode, "1", XMLVersion.Browslevel2);
                            global_Session._global_instance.BrowseLevel2 = XMLVersion.Browslevel2;
                            global_Session._global_instance.BrowseLevel3 = XMLVersion.Browslevel3;
                            //global_Session._global_instance.xmlDocument.Save(global_Session._global_instance.savepath);


                        }
                        else
                        {
                            Debugger.Break();

                        }
                    }


                }
            }
            else
            {

                string Log = FilePath + "|" + "Code Title not found\r\n";
                System.IO.File.AppendAllText(ErrorLogFile, Log);

            }



        }

        static void SaveOpinionInXml_New2(Opinion XMLVersion, string FilePath, string IncomingPath, string OutgoingPath, string ErrorLogFile)
        {
            string FullFilePath = XMLVersion.SaveFolderPath;
            FileInfo FInfo = new FileInfo(FullFilePath);
            string FileName = FInfo.Name;
            string FinalPath = FInfo.Directory.FullName;
            string VolumeName = XMLVersion.Browslevel1;
            if (VolumeName != null)
            {
                FinalPath = FinalPath + "\\" + VolumeName;
                string ProductionPath = string.Empty;


                if (!Directory.Exists(FinalPath))
                {
                    Directory.CreateDirectory(FinalPath);
                    string SaveXmlPath = FinalPath + "\\" + FileName;
                    secondary_xml_data_model objData = new secondary_xml_data_model();

                    objData.level_of_data = "2";

                    index_element_data_model objLevel1 = new index_element_data_model();
                    objLevel1.caption = XMLVersion.Browslevel1;
                    objLevel1.description = "";
                    objLevel1.sortorder = "1";
                    objLevel1.level = "1";
                    objLevel1.haschildlevel = "1";

                    objData.level1 = objLevel1;


                    index_element_data_model objLevelLastNode = new index_element_data_model();
                    objLevelLastNode.caption = "";
                    objLevelLastNode.description = XMLVersion.Browslevel2;
                    objLevelLastNode.sortorder = "1";
                    objLevelLastNode.shortname = XMLVersion.ShortName;
                    objLevelLastNode.revisionhistory = "";
                    objLevelLastNode.haschildlevel = "0";
                    objLevelLastNode.content = XMLVersion.HeaderHtml + "\n" + XMLVersion.OpinionHtml;
                    objLevelLastNode.level = "2";

                    objData.level2 = objLevelLastNode;


                    objData.edition_date = "01/01/2022";
                    objData.revision_date = "";
                    objData.currency_text = "2022";
                    objData.library_edition_description = XMLVersion.ShortName;
                    objData.library_SourceConst = "COBARJ";
                    objData.library_name = XMLVersion.LibraryName;

                    CreateXml(objData, SaveXmlPath);

                }
                else
                {
                    string[] AllXmlFile = Directory.GetFiles(FinalPath, "*.xml", SearchOption.TopDirectoryOnly);
                    if (AllXmlFile.Count() > 0)
                    {
                        string SaveXmlPath = AllXmlFile[0].ToString();
                        index_element_data_model objLevel = new index_element_data_model();
                        objLevel.caption = "";
                        objLevel.description = XMLVersion.Browslevel2;
                        objLevel.sortorder = "";
                        objLevel.shortname = XMLVersion.ShortName;
                        objLevel.revisionhistory = "";
                        objLevel.haschildlevel = "0";
                        objLevel.content = XMLVersion.HeaderHtml + "\n" + XMLVersion.OpinionHtml;
                        objLevel.level = "2";
                        UpdateXml(SaveXmlPath, objLevel);
                    }


                }
            }
            else
            {

                string Log = FilePath + "|" + "Code Title not found\r\n";
                System.IO.File.AppendAllText(ErrorLogFile, Log);

            }



        }



        static HeaderOpinion common_WCOMP_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";

            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();

            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_WCOMP_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();
            string DecisionDate = ReadMetaTag(Content, "Casedate2").Trim();
            if (DecisionDate.Trim() == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
            }
            DecisionDate = GetDecisionDate(DecisionDate);
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }
            if (DecisionDate.Length == 4)
            {
                DecisionDate = DecisionDate + "/01/01";
            }
            string PartyHeader = ReadMetaTag(Content, "Catchline").Trim();

            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.PartyHeader = PartyHeader;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_AGO_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";

            ShortName = ReadMetaTag(Content, "CodeSec").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_AGO_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "Search_Codesec").Trim();
            string DecisionDate = ReadMetaTag(Content, "Casedate2").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }
            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_EO_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";

            ShortName = ReadMetaTag(Content, "CodeSec").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_EO_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "OpinionSearch");
            string DecisionDate = ReadMetaTag(Content, "DateSearch").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }

            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_BARJ_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";
            bool shortNameFlag = false;

            string ShortName1 = ReadMetaTag(Content, "DocketNo").Trim();
            string ShortName2 = ReadMetaTag(Content, "Catchline").Trim();

            if (ShortName1.IndexOf(",") > -1)
            {
                ShortName1 = ShortName1.Substring(0, ShortName1.IndexOf(","));
            }
            else if (ShortName1 != "")
            {
                string[] ArrayData = Regex.Split(ShortName1, "\\s", RegexOptions.IgnoreCase);

                if (ArrayData.Count() > 2)
                {
                    ShortName1 = ArrayData[0].ToString() + " " + ArrayData[1].ToString();
                }
                else
                {
                    //System.Diagnostics.Debugger.Break();
                }

            }

            ShortName = ReadMetaTag(Content, "CodeSec").Trim();
            string DocketNumber = ReadMetaTag(Content, "CodeSec").Trim();
            string DecisionDate = ReadMetaTag(Content, "CaseDate2").Trim();

            StateName = ReadMetaTag(Content, "State").Trim();


            LibraryName = get_BARJ_Library_Name(StateName);

            if (DecisionDate == "" || (Regex.IsMatch(DecisionDate, @"\d{4}", RegexOptions.IgnoreCase) == true && DecisionDate.Length == 4))
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DateTime TDate1;
                if (DecisionDate == "")
                {
                    DecisionDate = ReadMetaTag(Content, "SessionYr").Trim();
                }
                else if (!DateTime.TryParse(DecisionDate, out TDate1))
                {
                    DecisionDate = ReadMetaTag(Content, "SessionYr").Trim();
                }
                DecisionDate = DecisionDate + "/01/01";
            }

            if (Regex.IsMatch(DecisionDate, @"\d{4}\\\d{2}\\\d{2}", RegexOptions.IgnoreCase) == true)
            {
                DecisionDate = DecisionDate.Replace("\\", "/");
            }
            DateTime TDate;

            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            if (Regex.IsMatch(ShortName, @"(Pg.)(\s|\n)*(\d|\w)+"))
            {
                string test = Regex.Match(ShortName, @"(Pg.)(\s|\n)*(\d|\w)+").Value;
                string pagenumber = test.Replace("Pg.", "").Trim();
                switch (pagenumber.Length)
                {
                    case 1:
                        test = test.Replace(pagenumber, "000" + pagenumber);
                        break;
                    case 2:
                        test = test.Replace(pagenumber, "00" + pagenumber);
                        break;
                    case 3:
                        test = test.Replace(pagenumber, "0" + pagenumber);
                        break;
                }
                ShortName = ShortName.Replace(Regex.Match(ShortName, @"(Pg.)(\s|\n)*(\d|\w)+").Value, test);
                if (Regex.IsMatch(ShortName, @"\d{4}(\s|\n)*(\,)*(\s|\n)*(January|February|March|April|May|June|July|August|September|October|November|December)"))
                {
                    string YearStr = Regex.Match(ShortName, @"\d{4}(\s|\n)*(\,)*(\s|\n)*(January|February|March|April|May|June|July|August|September|October|November|December)").Value;
                    string Month = Regex.Match(YearStr, "(January|February|March|April|May|June|July|August|September|October|November|December)").Value;
                    string Year = Regex.Match(YearStr, @"\d{4}").Value;
                    switch (Month)
                    {
                        case "January":
                            Month = "01";
                            break;
                        case "February":
                            Month = "02";
                            break;
                        case "March":
                            Month = "03";
                            break;
                        case "April":
                            Month = "04";
                            break;
                        case "May":
                            Month = "05";
                            break;
                        case "June":
                            Month = "06";
                            break;
                        case "July":
                            Month = "07";
                            break;
                        case "August":
                            Month = "08";
                            break;
                        case "September":
                            Month = "09";
                            break;
                        case "October":
                            Month = "10";
                            break;
                        case "November":
                            Month = "11";
                            break;
                        case "December":
                            Month = "12";
                            break;
                    }


                    if (StateName.Trim().ToLower() == "co" || StateName.Trim().ToLower() == "colorado")
                    {
                        string newstr = Year + "-" + Month;
                        DateTime date = new DateTime(Convert.ToInt32(Year), Convert.ToInt32(Month), 1);
                        string getFullMonthName = date.ToString("MMMM");
                        ShortName = ShortName.Replace(YearStr, newstr);
                        ShortName = ShortName + " (" + getFullMonthName + ", " + Year + ")";
                    }
                    else
                    {
                        string newstr = Year + "-" + Month;
                        ShortName = ShortName.Replace(YearStr, newstr);
                    }
                }
                else
                {
                    //System.Diagnostics.Debugger.Break();
                }
            }
            else
            {
                //System.Diagnostics.Debugger.Break();
                shortNameFlag = true;
            }

            if (shortNameFlag == true)
            {
                ShortName = ShortName + ", " + ShortName2;
            }
            else if (ShortName1.Trim() == "")
            {
                ShortName = ShortName + ", " + ShortName2;
            }
            else
            {
                ShortName = ShortName1 + ", " + ShortName + ", " + ShortName2;
            }

            //DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            string CodeTitle = ReadMetaTag(Content, "CodeTitle").Trim();
            string PartyHeader = ReadMetaTag(Content, "Catchline").Trim();
            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            objData.PartyHeader = PartyHeader;
            objData.VolumeName = CodeTitle;
            objData.Description = PartyHeader;
            objData.Caption = DocketNumber;
            return objData;
        }



        static HeaderOpinion common_NLRB_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";
            bool shortNameFlag = false;
            string Caption = "";

            ShortName = ReadMetaTag(Content, "TITLE");

            //Changed by Jubin 2023.01.13
            //In re Bob&#39;s Tire Co., 073119 NLRB, 01-CA-183476            
            ShortName = Regex.Replace(ShortName, @"\,\s*\d{6}\s+NLRB\,", ",");
            
            string ShortName1 = ReadMetaTag(Content, "DocketNo").Trim();
            string ShortName2 = ReadMetaTag(Content, "Catchline").Trim();

            Caption = ReadMetaTag(Content, "Citation").Trim();
            if (Caption.Trim()=="")
            {
                Caption = ReadMetaTag(Content, "NBCCite").Trim();
            }

            if (Caption=="")
            {
                //Debugger.Break();
            }

            string DocketNumber = ReadMetaTag(Content, "DocketNo").Trim();
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();

            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_NLRB_Library_Name(StateName);

            if (LibraryName=="")
            {
                //Debugger.Break();

                LibraryName = "National Labor Relations Board";

            }

            if (DecisionDate == "" || (Regex.IsMatch(DecisionDate, @"\d{4}", RegexOptions.IgnoreCase) == true && DecisionDate.Length == 4))
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DateTime TDate1;
                if (DecisionDate == "")
                {
                    DecisionDate = ReadMetaTag(Content, "SessionYr").Trim();
                }
                else if (!DateTime.TryParse(DecisionDate, out TDate1))
                {
                    DecisionDate = ReadMetaTag(Content, "SessionYr").Trim();
                }
                DecisionDate = DecisionDate + "/01/01";
            }

            if (Regex.IsMatch(DecisionDate, @"\d{4}\\\d{2}\\\d{2}", RegexOptions.IgnoreCase) == true)
            {
                DecisionDate = DecisionDate.Replace("\\", "/");
            }
            DateTime TDate;

            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }

            string CodeTitle = ReadMetaTag(Content, "CaseYear").Trim();
            string PartyHeader = ReadMetaTag(Content, "PartyName").Trim();
            string FullStateName = GetJSStateNameLibra("FEDNLRB");
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            objData.PartyHeader = PartyHeader;
            objData.VolumeName = CodeTitle;
            objData.Description = PartyHeader;
            objData.Caption = Caption;
            return objData;
        }


        static HeaderOpinion common_FEDREG_HeaderInfo(string Content, string OutputPath, string CaseCounter, federal_register_model ObjFed)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";
            string CodeTitle = "";
            string DocketNumber = "";
            string DecisionDate = "";
            string Pages = "";
            string Description = "";
            string Caption = "";
            StateName = "US";
            ShortName = ReadMetaTag(Content, "Publication_Title").Trim();
            Pages = ReadMetaTag(Content, "Page_Number_Range").Trim();
            DocketNumber = ReadMetaTag(Content, "Federal_Register_Citation").Trim();
            string PartyHeader = ReadMetaTag(Content, "Agency_Name").Trim();
            if (ShortName.Trim() == "")
            {
                //System.Diagnostics.Debugger.Break();
            }
            else
            {
                string Vol1 = "";
                string Issue1 = "";


                if (Regex.IsMatch(ShortName, "volume(\\s+)?\\d+", RegexOptions.IgnoreCase) == true)
                {
                    Vol1 = Regex.Match(ShortName, "volume(\\s+)?\\d+", RegexOptions.IgnoreCase).Value;
                }
                if (Regex.IsMatch(ShortName, "issue(\\s+)?\\d+", RegexOptions.IgnoreCase) == true)
                {
                    Issue1 = Regex.Match(ShortName, "issue(\\s+)?\\d+", RegexOptions.IgnoreCase).Value;
                }

                DecisionDate = GetDecisionDate(ShortName);
                CodeTitle = Vol1;
                ShortName = DecisionDate + " (" + Vol1 + ", " + Issue1 + ") (Pages " + Pages.Replace("-", " - ") + ")";
                Caption = DecisionDate;
                Description = " (" + Vol1 + ", " + Issue1 + ") (Pages " + Pages.Replace(" - ", " - ") + ")";
            }




            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "Publication_Session").Trim();
                if (DecisionDate.IndexOf("-") > 0)
                {
                    DecisionDate = DecisionDate.Substring(0, DecisionDate.IndexOf("-") + 1);
                }
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;

            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }
            string Browslevel2 = ReadMetaTag(Content, "CodeSec").Trim();

            LibraryName = get_FEDREG_Library_Name("FEDREG");

            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");
            string Month = "";
            string Year = "";

            if (ShortName.Trim() != "")
            {
                Month = Convert.ToDateTime(DecisionDate).ToString("MMMM");
                Year = Convert.ToDateTime(DecisionDate).Year.ToString();
            }
            else {
                Month = ObjFed.iMonth;
                Year = ObjFed.iYear;
            }
           

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            objData.PartyHeader = PartyHeader;
            objData.VolumeName = Year + "\\"+ CodeTitle;
            objData.Description = Description;
            objData.Caption = Caption;
            objData.Browslevel1 = Year;
            objData.Browslevel2 = Month;
            objData.Browslevel3 = DecisionDate;
            return objData;
        }


        static HeaderOpinion common_DSSUPM_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_DSSUPM_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "SessionYr").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }
            string Browslevel2 = ReadMetaTag(Content, "CodeSec").Trim();

            //DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            string CodeTitle = ReadMetaTag(Content, "CodeTitle").Trim();
            string PartyHeader = ReadMetaTag(Content, "Catchline").Trim();
            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            objData.PartyHeader = PartyHeader;
            objData.Browslevel1 = CodeTitle;
            objData.Description = PartyHeader;
            objData.Browslevel2 = Browslevel2;
            return objData;
        }

        static HeaderOpinion common_ETHICRG_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_ETHICRG_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "SessionYr").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }

            string CodeTitle = ReadMetaTag(Content, "CodeTitle").Trim();
            string PartyHeader = ReadMetaTag(Content, "Catchline").Trim();
            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            objData.PartyHeader = PartyHeader;
            objData.Browslevel1 = CodeTitle;
            objData.Description = PartyHeader;
            objData.Browslevel2 = ShortName;
            return objData;
        }

        static HeaderOpinion common_RG_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_RG_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "SessionYr").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }
            string Browslevel2 = ReadMetaTag(Content, "CodeSec").Trim();

            //DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            string CodeTitle = ReadMetaTag(Content, "CodeTitle").Trim();
            string PartyHeader = ReadMetaTag(Content, "Catchline").Trim();
            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            objData.PartyHeader = PartyHeader;
            objData.Browslevel1 = CodeTitle;
            objData.Description = PartyHeader;
            objData.Browslevel2 = ShortName;
            return objData;
        }


        static HeaderOpinion common_KYORD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_KYORD_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }

            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_KYOMD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_KYOMD_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }

            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_MCAIDORD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";

            ShortName = ReadMetaTag(Content, "ShortTitle").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_MCAIDORD_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }

            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_AdminCourt_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            //if (StateName.Trim()=="")
            //{
            //    StateName = "AK";

            //}

            LibraryName = get_AdminCourt_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_FOI_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            LibraryName = get_FOI_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_TAXCOURT_HeaderInfo(string Content, string OutputPath, string CaseCounter, string FilePath = "")
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();
            if (StateName == "RI")
            {
                ShortName = ReadMetaTag(Content, "CodeSec");
            }

            LibraryName = get_TAXCOURT_Library_Name(StateName);
            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;


            //string Decision_Date = ReadMetaTag(Content, "CaseDate1").Trim();
            //string Court_Name = ReadMetaTag(Content, "Court").Trim();
            //string Docket_Number = ReadMetaTag(Content, "DocketNo").Trim();
            //string PartyHeader = ReadMetaTag(Content, "PartyName").Trim();
            //string Short_Name = ReadMetaTag(Content, "ShortTitle").Trim();

            //if (Decision_Date.Trim() == "")
            //{
            //    string abc = "";

            //}

            //if (Court_Name.Trim() == "")
            //{
            //    string abc = "";

            //}

            //if (Docket_Number.Trim() == "")
            //{
            //    string abc = "";

            //}

            //if (PartyHeader.Trim() == "")
            //{
            //    string abc = "";

            //}
            //if (Short_Name.Trim() == "")
            //{
            //    string abc = "";

            //}
            //string AllContentData = DecisionDate + "|" + Court_Name + "|" + Docket_Number + "|" + PartyHeader + "|" + Short_Name + "|" + FilePath + "\n";

            //File.AppendAllText(@"\\192.168.1.232\f$\NEWDATA\1_Udate\Migrating state content to Fastcase XML\02.02.2022\LogData\INTAXCourt.txt", AllContentData);




            return objData;
        }

        static HeaderOpinion common_SUPCOURTDECISION_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_SUPCOURTDECISION_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_CHR_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_CHR_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_WCD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_WCD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_PUC_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_PUC_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_EHB_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_EHB_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_SUPCO_HeaderInfo(string Content, string OutputPath, string CaseCounter, string FilePath = "")
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_SUPCO_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;


            //string Decision_Date = ReadMetaTag(Content, "CaseDate1").Trim();
            //string Court_Name = ReadMetaTag(Content, "Court").Trim();
            //string Docket_Number = ReadMetaTag(Content, "DocketNo").Trim();
            //string PartyHeader = ReadMetaTag(Content, "PartyName").Trim();
            //string Short_Name = ReadMetaTag(Content, "ShortTitle").Trim();

            //if (Decision_Date.Trim() == "")
            //{
            //    string abc = "";

            //}

            //if (Court_Name.Trim() == "")
            //{
            //    string abc = "";

            //}

            //if (Docket_Number.Trim() == "")
            //{
            //    string abc = "";

            //}

            //if (PartyHeader.Trim() == "")
            //{
            //    string abc = "";

            //}
            //if (Short_Name.Trim() == "")
            //{
            //    string abc = "";

            //}
            //string AllContentData = DecisionDate + "|" + Court_Name + "|" + Docket_Number + "|" + PartyHeader + "|" + Short_Name + "|" + FilePath + "\n";

            //File.AppendAllText(@"\\192.168.1.232\f$\NEWDATA\1_Udate\Migrating state content to Fastcase XML\02.02.2022\LogData\RISuperior.txt", AllContentData);


            return objData;
        }

        static HeaderOpinion common_PEDBD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_PEDBD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_GMCBD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_GMCBD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_HSBD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_HSBD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_VD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_VD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_PCD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_PCD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_GMD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_GMD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_PUBUTL_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_PUBUTL_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_ENVBD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_ENVBD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_FAA_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_FAA_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate2").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_NHCHR_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_NHCHR_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate2").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_HICIVRCOM_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_HICIVRCOM_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }

        static HeaderOpinion common_IRD_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_IRD_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "DateAdded").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "Effective_At").Trim();
                //DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_IRS_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "RuleSearch");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_IRS_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "DecidedDate").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "DecidedDateNum").Trim();
                //DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }


        static HeaderOpinion common_MSPSC_HeaderInfo(string Content, string OutputPath, string CaseCounter)
        {
            HeaderOpinion objData = new HeaderOpinion();
            string ShortName = "";
            string StateName = "";
            string LibraryName = "";


            ShortName = ReadMetaTag(Content, "title");
            ShortName = Regex.Replace(ShortName, "\r\n ", "").Trim();
            ShortName = Regex.Replace(ShortName, "\\s+", " ").Trim();
            if (ShortName.Trim() == "")
            {
                System.Diagnostics.Debugger.Break();
            }
            StateName = ReadMetaTag(Content, "State").Trim();

            LibraryName = get_MSPSC_Library_Name(StateName);

            string DocketNumber = ReadMetaTag(Content, "DocketNo");
            string DecisionDate = ReadMetaTag(Content, "CaseDate1").Trim();
            DecisionDate = GetDecisionDate(DecisionDate);
            if (DecisionDate == "")
            {
                DecisionDate = ReadMetaTag(Content, "CodeTitle").Trim();
                DecisionDate = DecisionDate + "/01/01";
            }
            DateTime TDate;
            if (!DateTime.TryParse(DecisionDate, out TDate))
            {
                DecisionDate = "Casedate2|" + DecisionDate;
            }
            else
            {
                DecisionDate = Convert.ToDateTime(DecisionDate).ToString("yyyy/MM/dd");
            }



            string FullStateName = GetJSStateNameLibra(StateName);
            string SavePath = Path.Combine(OutputPath, FullStateName, LibraryName, "fcjsm-s" + DateTime.Now.ToString("yyMMdd") + CaseCounter.ToString().PadLeft(4, '0') + ".xml");

            objData.ShortName = ShortName;
            objData.DocketNumber = DocketNumber;
            objData.DecisionDate = DecisionDate;
            objData.LibraryName = LibraryName;
            objData.SaveFolderPath = SavePath;
            return objData;
        }




        static string get_IRD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "FEDIRD":
                    libraryName = "Internal Revenue Bulletin";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_IRS_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "US":
                    libraryName = "Internal Revenue Service";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_MSPSC_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "MS":
                    libraryName = "MSPSC";
                    break;

                default:
                    break;
            }
            return libraryName;


        }
        static string get_HICIVRCOM_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "HI":
                    libraryName = "HICIVRCOM";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_FAA_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "FEDFAA":
                    libraryName = "Federal Aviation Administration Office of the Chief Counsel";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_NHCHR_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "NH":
                    libraryName = "The New Hampshire Commission For Human Rights";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_WCOMP_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "AK":
                    libraryName = "Alaska Worker's Compensation Decisions";
                    break;
                case "AR":
                    libraryName = "Arkansas Worker's Compensation Opinions";
                    break;
                case "CO":
                    libraryName = "Colorado Workers' Compensation Decisions";
                    break;
                case "CA":
                    libraryName = "California Worker's Compensation Decisions";
                    break;
                case "CT":
                    libraryName = "Connecticut Worker's Compensation Decisions";
                    break;

                case "DE":
                    // libraryName = "Delaware Worker's Compensation Decisions";
                    libraryName = "Delaware Workers Compensation Decisions";
                    break;

                case "ID":
                    //libraryName = "Idaho Worker's Compensation Decisions";
                    libraryName = "Idaho Industrial Commission Decisions";
                    break;

                case "IA":
                    libraryName = "Iowa Worker's Compensation Decisions";
                    break;

                case "MA":
                    //libraryName = "Massachusetts Worker's Compensation Decisions";
                    libraryName = "Massachusetts Workers Compensation Decisions";
                    break;

                case "MO":
                    //libraryName = "Missouri Worker's Compensation Decisions";
                    libraryName = "Missouri Workers' Compensation Decisions";
                    break;

                case "VA":
                    // libraryName = "Virginia Worker's Compensation Decisions";
                    libraryName = "Virginia Workers' Compensation Decisions";
                    break;

                case "KS":
                    libraryName = "Kansas Worker's Compensation Decisions";
                    break;
                case "KY":
                    libraryName = "Kentucky Worker's Compensation Opinions";
                    break;
                case "MI":
                    libraryName = "Michigan Worker's Compensation Opinions";
                    break;
                case "ME":
                    libraryName = "Maine Worker's Compensation Decisions";
                    break;
                case "MN":
                    libraryName = "Minnesota Worker's Compensation Decisions";
                    break;
                case "NE":
                    //libraryName = "Nebraska Worker's Compensation Decisions";
                    libraryName = "Nebraska Workers' Compensation Decisions";
                    break;
                case "NJ":
                    libraryName = "New Jersey Worker's Compensation Decisions";
                    break;
                case "NY":
                    libraryName = "New York Worker's Compensation Decisions";
                    break;
                case "OR":
                    libraryName = "Oregon Worker's Compensation Decisions";
                    break;
                case "RI":
                    libraryName = "Rhode Island Worker's Compensation Decisions";
                    break;

                case "SD":
                    libraryName = "South Dakota Worker's Compensation Decisions";
                    break;

                case "TN":
                    libraryName = "Tennessee Worker's Compensation Decisions";
                    break;

                case "UT":
                    libraryName = "Utah Worker's Compensation Decisions";
                    break;
                case "VT":
                    libraryName = "Vermont Workers' Compensation Decisions";
                    break;

                case "WI":
                    libraryName = "Wisconsin Worker's Compensation Decisions";
                    break;

                case "MS":
                    // libraryName = "Mississippi Worker's Compensation Opinions";
                    libraryName = "Mississippi Workers’ Compensation Opinions";
                    break;

                default:
                    break;
            }
            return libraryName;
        }

        static string get_AGO_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {
                case "AL":
                    libraryName = "Alabama Attorney General Opinions";
                    break;

                case "AK":
                    libraryName = "Alaska Attorney General Opinions";
                    break;

                case "CA":
                    //libraryName = "California Attorney General Opinion";
                    libraryName = "California Attorney General Opinions";
                    break;

                case "CO":
                    libraryName = "Colorado Attorney General Opinion";
                    break;

                case "CT":
                    libraryName = "Connecticut Attorney General Opinions";
                    break;


                case "GA":
                    libraryName = "Georgia Attorney General Opinion";
                    break;

                case "HI":
                    libraryName = "Hawaii Attorney General Opinions";
                    break;

                case "ID":
                    libraryName = "Idaho Attorney General Opinion";
                    break;

                case "IL":
                    libraryName = "Illinois Attorney General Opinion";
                    break;

                case "IN":
                    // libraryName = "Indiana Attorney General Opinion";
                    libraryName = "Indiana Attorney General Opinions";
                    break;

                case "KS":
                    libraryName = "Kansas Attorney General Opinions";
                    break;

                case "KY":
                    libraryName = "Kentucky Attorney General Opinions";
                    break;

                case "ME":
                    libraryName = "Maine Attorney General Opinion";
                    break;

                case "MD":
                    // libraryName = "Maryland Attorney General Opinion";
                    libraryName = "Maryland Attorney General Opinions";
                    break;

                case "MA":
                    libraryName = "Massachusetts Attorney General Opinions";
                    break;

                case "MI":
                    libraryName = "Michigan Attorney General Opinions";
                    break;

                case "MN":
                    libraryName = "Minnesota Attorney General Opinions";
                    break;

                case "MS":
                    libraryName = "Mississippi Attorney General Opinions";
                    break;
                case "MO":
                    libraryName = "Attorney General Of Missouri";
                    break;

                case "MT":
                    libraryName = "Montana Attorney General Opinion";
                    break;

                case "NE":
                    libraryName = "Nebraska Attorney General Opinions";
                    break;

                case "NJ":
                    libraryName = "New Jersey Attorney General Opinions";
                    break;

                case "NM":
                    libraryName = "New Mexico Attorney General Opinions";
                    break;


                case "NY":
                    libraryName = "New York Attorney General Opinions";
                    break;

                case "ND":
                    libraryName = "North Dakota Attorney General Opinions";
                    break;


                case "OH":
                    libraryName = "Ohio Attorney General Opinions";
                    break;

                case "OR":
                    libraryName = "Oregon Attorney General Opinions";
                    break;

                case "PA":
                    libraryName = "Pennsylvania Attorney General Opinions";
                    break;

                case "RI":
                    libraryName = "Rhode Island Attorney General Opinions";
                    break;

                case "SC":
                    libraryName = "South Carolina Attorney General Opinions";
                    break;

                case "SD":
                    libraryName = "South Dakota Attorney General Opinions";
                    break;

                case "TN":
                    libraryName = "Tennessee Attorney General Opinions";
                    break;

                case "TX":
                    libraryName = "Texas Attorney General Opinions";
                    break;

                case "WA":
                    libraryName = "Washington Attorney General Opinions";
                    break;

                case "WV":
                    libraryName = "West Virginia Attorney General Opinions";
                    break;

                case "WY":
                    libraryName = "Wyoming Attorney General Opinions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_EO_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {
                case "AL":
                    libraryName = "Alabama Ethics Opinions";
                    break;

                case "AK":
                    libraryName = "Alaska Ethics Opinions";
                    break;

                case "CA":
                    libraryName = "California Ethics Opinions";
                    break;

                case "CO":
                    libraryName = "Colorado Ethics Opinions";
                    break;

                case "CT":
                    libraryName = "Connecticut Ethics Opinions";
                    break;

                case "FL":
                    libraryName = "Florida Ethics Opinions";
                    break;

                case "GA":
                    libraryName = "Georgia Ethics Opinions";
                    break;

                case "HI":
                    libraryName = "Hawaii Ethics Opinions";
                    break;

                case "ID":
                    libraryName = "Idaho Ethics Opinions";
                    break;

                case "IL":
                    libraryName = "Illinois Ethics Opinions";
                    break;

                case "IN":
                    libraryName = "Indiana Ethics Opinion";
                    break;

                case "KS":
                    libraryName = "Kansas Ethics Opinions";
                    break;

                case "KY":
                    libraryName = "Kentucky Ethics Opinion";
                    break;

                case "ME":
                    libraryName = "Maine Ethics Opinions";
                    break;

                case "MD":
                    libraryName = "Maryland Ethics Opinions";
                    break;

                case "MA":
                    libraryName = "Massachusetts Ethics Opinions";
                    break;

                case "MI":
                    libraryName = "Michigan Attorney General Opinions";
                    break;

                case "MN":
                    libraryName = "Minnesota Ethics Opinions";
                    break;

                case "MS":
                    // libraryName = "Mississippi Ethics Opinion";
                    libraryName = "Mississippi Ethics Opinions";
                    break;
                case "MO":
                    libraryName = "Missouri Ethics Opinions";
                    break;

                case "MT":
                    libraryName = "Montana Ethics Opinions";
                    break;

                case "NE":
                    libraryName = "Nebraska Ethics Opinions";
                    break;

                case "NH":
                    libraryName = "NH Ethics Opinions";
                    break;

                case "NJ":
                    libraryName = "New Jersey Ethics Opinions";
                    break;

                case "NM":
                    libraryName = "New Mexico Ethics Opinions";
                    break;


                case "NY":
                    //libraryName = "New York Ethics Opinions";
                    libraryName = "New York Bar Ethics Opinions";
                    break;

                case "ND":
                    libraryName = "North Dakota Ethics Opinions";
                    break;


                case "OH":
                    libraryName = "Ohio Advisory Opinions";
                    break;

                case "OR":
                    libraryName = "Oregon Ethics Opinions";
                    break;

                case "PA":
                    libraryName = "Pennsylvania State Ethics Opinions";
                    break;

                case "RI":
                    //libraryName = "Rhode Island Ethics Opinions";
                    libraryName = "Rhode Island Advisory Opinions";
                    break;

                case "SC":
                    libraryName = "South Carolina Ethics Opinions";
                    break;

                case "SD":
                    libraryName = "South Dakota Ethics Opinions";
                    break;

                case "TN":
                    libraryName = "Tennessee Ethics Opinions";
                    break;

                case "TX":
                    libraryName = "Texas Ethics Opinions";
                    break;

                case "UT":
                    libraryName = "Texas Ethics Opinions";
                    break;

                case "VT":
                    libraryName = "Vermont Advisory Ethics Opinions";
                    break;

                case "WA":
                    libraryName = "Washington Ethics Opinions";
                    break;

                case "WV":
                    libraryName = "West Virginia Ethics Opinions";
                    break;

                case "WY":
                    libraryName = "Wyoming Ethics Opinions";
                    break;



                default:
                    break;
            }
            return libraryName;


        }

        static string get_BARJ_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {
                case "AL":
                    libraryName = "Alabama Bar Journal";
                    break;
                case "AK":
                    libraryName = "Alaska Bar Journal";
                    break;
                case "AR":
                    libraryName = "Arkansas Bar Journal";
                    break;
                case "CO":
                    libraryName = "Colorado Bar Journal";
                    break;
                case "CA":
                    libraryName = "California Bar Journal";
                    break;
                case "CT":
                    libraryName = "Connecticut Bar Journal";
                    break;

                case "DE":
                    libraryName = "Delaware Bar Journal";
                    break;

                case "GA":
                    libraryName = "Georgia Bar Journal";
                    break;

                case "HI":
                    libraryName = "Hawaii Bar Journal";
                    break;

                case "ID":
                    libraryName = "Idaho Bar Journal";
                    break;

                case "IA":
                    libraryName = "Iowa Bar Journal";
                    break;

                case "MA":
                    libraryName = "Massachusetts Bar Journal";
                    break;

                case "MO":
                    libraryName = "Missouri Bar Journal";
                    break;

                case "VA":
                    libraryName = "Virginia Bar Journal";
                    break;

                case "KS":
                    libraryName = "Kansas Bar Journal";
                    break;
                case "KY":
                    libraryName = "Kentucky Bar Journal";
                    break;
                case "MI":
                    libraryName = "Michigan Bar Journal";
                    break;
                case "ME":
                    libraryName = "Maine Bar Journal";
                    break;
                case "MN":
                    libraryName = "Minnesota Bar Journal";
                    break;
                case "NE":
                    libraryName = "Nebraska Bar Journal";
                    break;
                case "NH":
                    libraryName = "New Hampshire  Bar Journal";
                    break;

                case "NJ":
                    libraryName = "New Jersey Bar Journal";
                    break;
                case "NY":
                    libraryName = "New York Bar Journal";
                    break;
                case "OR":
                    libraryName = "Oregon Bar Journal";
                    break;
                case "RI":
                    libraryName = "Rhode Island Bar Journal";
                    break;

                case "SD":
                    libraryName = "South Dakota Bar Journal";
                    break;

                case "SC":
                    libraryName = "South Carolina  Bar Journal";
                    break;

                case "TN":
                    libraryName = "Tennessee Bar Journal";
                    break;

                case "UT":
                    libraryName = "Utah Bar Journal";
                    break;
                case "VT":
                    libraryName = "Vermont Bar Journal";
                    break;

                case "WI":
                    libraryName = "Wisconsin Bar Journal";
                    break;

                case "WY":
                    // libraryName = "Wyoming  Bar Journal";
                    libraryName = "Wyoming  Lawyer";
                    break;
                case "MS":
                    libraryName = "Mississippi Bar Journal";
                    break;

                default:
                    break;
            }
            return libraryName;
        }

        static string get_NLRB_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {
                case "FEDNLRB":
                    libraryName = "National Labor Relations Board";
                    break;
                case "FEDNLRB_CITED":
                    libraryName = "National Labor Relations Board";
                    break;

                default:
                    break;
            }
            return libraryName;
        }



        static string get_FEDREG_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {
                case "FEDREG":
                    libraryName = "Federal Register";
                    break;

                default:
                    break;
            }
            return libraryName;
        }



        static string get_MCAIDORD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "KY":
                    libraryName = "Administrative Hearings Branch";
                    break;

                default:
                    break;
            }
            return libraryName;


        }
        static string get_KYOMD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "KY":
                    libraryName = "Kentucky Open Meeting Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_KYORD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "KY":
                    libraryName = "Kentucky Open Records Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_DSSUPM_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {
                case "CT":
                    libraryName = "DSS Uniform Policy Manual";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_ETHICRG_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {
                case "IN":
                    libraryName = "Ethics Curbstone or Res Gestae";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_RG_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {
                case "IN":
                    libraryName = "Res Gestae";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_AdminCourt_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "AK":
                    libraryName = "Administrative Court Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_FOI_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "CT":
                    libraryName = "FOI Com Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }



        static string get_TAXCOURT_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "IN":
                    libraryName = "Indiana Tax Court Opinions";
                    break;
                case "KS":
                    libraryName = "Kansas Tax Court Opinions";
                    break;
                case "RI":
                    libraryName = "Rhode Island Tax Court Opinions";
                    break;
                default:
                    break;
            }
            return libraryName;


        }

        static string get_SUPCOURTDECISION_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "ME":
                    libraryName = "Superior Court Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_CHR_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "NH":
                    libraryName = "Commission on Human Rights";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_WCD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "NH":
                    libraryName = "Labor Wage & Hour Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_PUC_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "NH":
                    libraryName = "Public Utility Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_EHB_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "PA":
                    libraryName = "Environmental Hearing Board";
                    break;

                default:
                    break;
            }
            return libraryName;


        }


        static string get_SUPCO_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "RI":
                    libraryName = "Superior Court Opinions";
                    break;

                case "VT":
                    libraryName = "Vermont Superior Court Opinions";
                    break;
                default:
                    break;
            }
            return libraryName;


        }

        static string get_PEDBD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "VT":
                    libraryName = "Vermont Education Board Orders";
                    break;

                default:
                    break;
            }
            return libraryName;


        }



        static string get_GMCBD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "VT":
                    libraryName = "Vermont Green Mountain Care Board Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_HSBD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "VT":
                    libraryName = "Vermont Human Services Board Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_PCD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "VT":
                    libraryName = "Vermont Professional Responsibility Board Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_VD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "VT":
                    libraryName = "Vermont Vermont Digest";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_GMD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "WA":
                    libraryName = "Washington Growth Management Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_PUBUTL_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "ME":
                    libraryName = "Maine Public Utilities Commission";
                    break;

                default:
                    break;
            }
            return libraryName;


        }

        static string get_ENVBD_Library_Name(string stateName)
        {
            string libraryName = "";
            stateName = stateName.ToUpper();
            switch (stateName)
            {

                case "WA":
                    libraryName = "Washington Environmental Board Decisions";
                    break;

                default:
                    break;
            }
            return libraryName;


        }




        static string GetJSStateNameLibra(string pStateCd)
        {
            string lStateName;
            if (pStateCd.Trim().Substring(0, 1).ToUpper() == "BK")
                pStateCd = "BK";
            switch (pStateCd.ToUpper().Trim())
            {
                case "ALI":
                    {
                        lStateName = "Aliaba";
                        break;
                    }

                case "AL":
                    {
                        lStateName = "Alabama";
                        break;
                    }

                case "AK":
                    {
                        lStateName = "Alaska";
                        break;
                    }

                case "AZ":
                    {
                        lStateName = "Arizona";
                        break;
                    }

                case "AR":
                    {
                        lStateName = "Arkansas";
                        break;
                    }

                case "AB":
                    {
                        lStateName = "American Bar Association";
                        break;
                    }

                case "BK":
                    {
                        lStateName = "Bankruptcy";
                        break;
                    }

                case "BK*":
                    {
                        lStateName = "Bankruptcy";
                        break;
                    }

                case "CA":
                case "SCCBA":
                case "LACBA":
                    {
                        lStateName = "California";
                        break;
                    }

                case "CO":
                    {
                        lStateName = "Colorado";
                        break;
                    }

                case "CT":
                    {
                        lStateName = "Connecticut";
                        break;
                    }

                case "DE":
                    {
                        lStateName = "Delaware";
                        break;
                    }

                case "CD":
                case "DC":
                    {
                        lStateName = "District of Columbia";
                        break;
                    }

                case "FED":
                case "FED*":
                    {
                        lStateName = "U.S. Circuit Court";
                        break;
                    }

                case "DCFED":
                case "DCFED*":
                case "FEDDC":
                    {
                        lStateName = "U.S. District Court";
                        break;
                    }

                case "FEDSC":
                case "FEDSC*":
                    {
                        lStateName = "U.S. Supreme Court";
                        break;
                    }

                case "TRIBALCOURT":
                    {
                        lStateName = "Tribal Court";
                        break;
                    }

                case "FRDFED":
                    {
                        lStateName = "Federal Rules Decision";
                        break;
                    }

                //FEDIRD
                case "FEDIRD":
                    {
                        lStateName = "Federal Internal Revenue Bulletin";
                        break;
                    }
                case "FL":
                    {
                        lStateName = "Florida";
                        break;
                    }

                case "GA":
                    {
                        lStateName = "Georgia";
                        break;
                    }

                case "HLRA":
                    {
                        lStateName = "Harvard Law Review Association";
                        break;
                    }

                case "GU":
                    {
                        lStateName = "Guam";
                        break;
                    }

                case "HI":
                    {
                        lStateName = "Hawaii";
                        break;
                    }

                case "ID":
                    {
                        lStateName = "Idaho";
                        break;
                    }

                case "IL":
                    {
                        lStateName = "Illinois";
                        break;
                    }

                case "IN":
                    {
                        lStateName = "Indiana";
                        break;
                    }

                case "IA":
                    {
                        lStateName = "Iowa";
                        break;
                    }

                case "KS":
                    {
                        lStateName = "Kansas";
                        break;
                    }

                case "KY":
                    {
                        lStateName = "Kentucky";
                        break;
                    }

                case "LA":
                    {
                        lStateName = "Louisiana";
                        break;
                    }

                case "ME":
                    {
                        lStateName = "Maine";
                        break;
                    }

                case "MD":
                    {
                        lStateName = "Maryland";
                        break;
                    }

                case "MA":
                    {
                        lStateName = "Massachusetts";
                        break;
                    }

                case "MI":
                    {
                        lStateName = "Michigan";
                        break;
                    }

                case "ML":
                    {
                        lStateName = "Military Court";
                        break;
                    }

                case "MN":
                    {
                        lStateName = "Minnesota";
                        break;
                    }

                case "MS":
                    {
                        lStateName = "Mississippi";
                        break;
                    }

                case "MO":
                    {
                        lStateName = "Missouri";
                        break;
                    }

                case "MT":
                    {
                        lStateName = "Montana";
                        break;
                    }

                case "NBA":
                    {
                        lStateName = "National Bar Association";
                        break;
                    }

                case "NE":
                    {
                        lStateName = "Nebraska";
                        break;
                    }

                case "NV":
                    {
                        lStateName = "Nevada";
                        break;
                    }

                case "NH":
                    {
                        lStateName = "New Hampshire";
                        break;
                    }

                case "NJ":
                    {
                        lStateName = "New Jersey";
                        break;
                    }

                case "NM":
                    {
                        lStateName = "New Mexico";
                        break;
                    }

                case "NY":
                    {
                        lStateName = "New York";
                        break;
                    }

                case "YC":
                    {
                        lStateName = "New York City Bar";
                        break;
                    }

                case "NC":
                    {
                        lStateName = "North Carolina";
                        break;
                    }

                case "ND":
                    {
                        lStateName = "North Dakota";
                        break;
                    }

                case "OH":
                    {
                        lStateName = "Ohio";
                        break;
                    }

                case "OK":
                    {
                        lStateName = "Oklahoma";
                        break;
                    }

                case "OR":
                    {
                        lStateName = "Oregon";
                        break;
                    }

                case "PA":
                    {
                        lStateName = "Pennsylvania";
                        break;
                    }

                case "PR":
                    {
                        lStateName = "Puerto Rico";
                        break;
                    }

                case "RI":
                    {
                        lStateName = "Rhode Island";
                        break;
                    }

                case "RMMLF":
                case "RM":
                    {
                        lStateName = "Rocky Mountain Mineral Law Foundation";
                        break;
                    }

                case "RPCL":
                    {
                        lStateName = "Rulebook powered by Casemaker Libra";
                        break;
                    }

                case "APA":
                    {
                        lStateName = "American Psychological Association";
                        break;
                    }

                case "TOWER":
                    {
                        lStateName = "Tower Publishing";
                        break;
                    }

                case "SC":
                    {
                        lStateName = "South Carolina";
                        break;
                    }

                case "SD":
                    {
                        lStateName = "South Dakota";
                        break;
                    }

                case "TN":
                    {
                        lStateName = "Tennessee";
                        break;
                    }

                case "TX":
                    {
                        lStateName = "Texas";
                        break;
                    }

                case "TR":
                    {
                        lStateName = "Tribal Laws";
                        break;
                    }

                case "US":
                    {
                        lStateName = "Federal";
                        break;
                    }

                case "UT":
                    {
                        lStateName = "Utah";
                        break;
                    }

                case "VT":
                    {
                        lStateName = "Vermont";
                        break;
                    }

                case "VA":
                    {
                        lStateName = "Virginia";
                        break;
                    }

                case "VI":
                    {
                        lStateName = "Virgin Islands";
                        break;
                    }

                //case "PR":
                //    {
                //        lStateName = "Puerto Rico";
                //        break;
                //    }

                case "WA":
                    {
                        lStateName = "Washington";
                        break;
                    }

                case "WV":
                    {
                        lStateName = "West Virginia";
                        break;
                    }

                case "WI":
                    {
                        lStateName = "Wisconsin";
                        break;
                    }

                case "WY":
                    {
                        lStateName = "Wyoming";
                        break;
                    }

                case "FED01":
                    {
                        lStateName = "First Circuit";
                        break;
                    }

                case "FED02":
                    {
                        lStateName = "Second Circuit";
                        break;
                    }

                case "FED03":
                    {
                        lStateName = "Third Circuit";
                        break;
                    }

                case "FED04":
                    {
                        lStateName = "Fourth Circuit";
                        break;
                    }

                case "FED05":
                    {
                        lStateName = "Fifth Circuit";
                        break;
                    }

                case "FED06":
                    {
                        lStateName = "Sixth Circuit";
                        break;
                    }

                case "FED07":
                    {
                        lStateName = "Seventh Circuit";
                        break;
                    }

                case "FED08":
                    {
                        lStateName = "Eighth Circuit";
                        break;
                    }

                case "FED09":
                    {
                        lStateName = "Ninth Circuit";
                        break;
                    }

                case "FED10":
                    {
                        lStateName = "Tenth Circuit";
                        break;
                    }

                case "FED11":
                    {
                        lStateName = "Eleventh Circuit";
                        break;
                    }

                case "DCFED01":
                    {
                        lStateName = "First District";
                        break;
                    }

                case "DCFED02":
                    {
                        lStateName = "Second District";
                        break;
                    }

                case "DCFED03":
                    {
                        lStateName = "Third District";
                        break;
                    }

                case "DCFED04":
                    {
                        lStateName = "Fourth District";
                        break;
                    }

                case "DCFED05":
                    {
                        lStateName = "Fifth District";
                        break;
                    }

                case "DCFED06":
                    {
                        lStateName = "Sixth District";
                        break;
                    }

                case "DCFED07":
                    {
                        lStateName = "Seventh District";
                        break;
                    }

                case "DCFED08":
                    {
                        lStateName = "Eighth District";
                        break;
                    }

                case "DCFED09":
                    {
                        lStateName = "Ninth District";
                        break;
                    }

                case "DCFED10":
                    {
                        lStateName = "Tenth District";
                        break;
                    }

                case "DCFED11":
                    {
                        lStateName = "Eleventh District";
                        break;
                    }

                //case "BK":
                //case "BK*":
                //    {
                //        lStateName = "Bankruptcy";
                //        break;
                //    }

                //case "FEDSC":
                //    {
                //        lStateName = "Supreme Court";
                //        break;
                //    }

                case "IRD":
                    {
                        lStateName = "Internal Revenue Department";
                        break;
                    }

                case "TREASURY":
                    {
                        lStateName = "Treasury";
                        break;
                    }

                case "SEC":
                    {
                        lStateName = "Security and Exchange Commission";
                        break;
                    }

                case "FEDFAA":
                    {
                        lStateName = "FEDFAA";
                        break;
                    }

                case "FEDNLRB":
                    {
                        lStateName = "FEDNLRB";
                        break;
                    }
                //case "TOWER":
                //    {
                //        lStateName = "Tower Publishing";
                //        break;
                //    }

                default:
                    {
                        lStateName = "* The State code (" + pStateCd + ") could not be converted";
                        break;
                    }
            }
            return lStateName;
        }


        static Boolean validateDate(DateTime dtValue)
        {
            Boolean flagData = false;
            string currentYear = DateTime.Now.ToString("yyyy");
            string year = dtValue.ToString("yyyy");
            if (Convert.ToInt16(year) >= 1700 && Convert.ToInt16(year) <= Convert.ToInt16(currentYear) && dtValue <= DateTime.Now)
            {
                flagData = true;
            }
            else
            {

                flagData = false;
            }

            return flagData;
        }


        static void CreateXml(secondary_xml_data_model objXmlData, string savePath)
        {

            XmlDocument objXmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = objXmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            XmlElement rootNode = objXmlDoc.CreateElement("Content");
            rootNode.SetAttribute("Type", "Regulations");
            objXmlDoc.InsertBefore(xmlDeclaration, objXmlDoc.DocumentElement);

            if (objXmlData.level_of_data == "1")
            {
                XmlElement indexesNode1 = IndexesElementWithContent(objXmlDoc, objXmlData.level1);
                rootNode.AppendChild(indexesNode1);
            }
            else if (objXmlData.level_of_data == "2")
            {
                XmlElement indexesNodeContent2 = IndexesElementWithContent(objXmlDoc, objXmlData.level2);
                XmlElement indexesNode1 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent2, objXmlData.level1);
                rootNode.AppendChild(indexesNode1);
            }
            else if (objXmlData.level_of_data == "3")
            {
                XmlElement indexesNodeContent3 = IndexesElementWithContent(objXmlDoc, objXmlData.level3);
                XmlElement indexesNodeContent2 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent3, objXmlData.level2);
                XmlElement indexesNode1 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent2, objXmlData.level1);
                rootNode.AppendChild(indexesNode1);
            }

            else if (objXmlData.level_of_data == "4")
            {
                XmlElement indexesNodeContent4 = IndexesElementWithContent(objXmlDoc, objXmlData.level4);
                XmlElement indexesNodeContent3 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent4, objXmlData.level3);
                XmlElement indexesNodeContent2 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent3, objXmlData.level2);
                XmlElement indexesNode1 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent2, objXmlData.level1);
                rootNode.AppendChild(indexesNode1);
            }

            XmlElement EditionDateNode = objXmlDoc.CreateElement("EditionDate");
            EditionDateNode.InnerText = objXmlData.edition_date;
            XmlElement RevisionDateNode = objXmlDoc.CreateElement("RevisionDate");
            RevisionDateNode.InnerText = objXmlData.revision_date;
            XmlElement CurrencyTextNode = objXmlDoc.CreateElement("CurrencyText");
            CurrencyTextNode.InnerText = objXmlData.currency_text;
            XmlElement LibraryEditionDescriptionNode = objXmlDoc.CreateElement("LibraryEditionDescription");
            LibraryEditionDescriptionNode.InnerText = objXmlData.library_edition_description;
            XmlElement LibrarySourceConstNode = objXmlDoc.CreateElement("LibrarySourceConst");
            LibrarySourceConstNode.InnerText = objXmlData.library_SourceConst;
            XmlElement LibraryNameNode = objXmlDoc.CreateElement("LibraryName");
            LibraryNameNode.InnerText = objXmlData.library_name;

            rootNode.AppendChild(EditionDateNode);
            rootNode.AppendChild(RevisionDateNode);
            rootNode.AppendChild(CurrencyTextNode);
            rootNode.AppendChild(LibraryEditionDescriptionNode);
            rootNode.AppendChild(LibrarySourceConstNode);
            rootNode.AppendChild(LibraryNameNode);

            objXmlDoc.AppendChild(rootNode);


            // Save to the XML file
            objXmlDoc.Save(savePath);
            if (global_Session._global_instance.savepath == null)
            {
                global_Session._global_instance.savepath = savePath;
            }

            global_Session._global_instance.xmlDocument = objXmlDoc;
        }



        static void UpdateXml(string savePath, index_element_data_model objElement, string CaptionValue = "")
        {
            string filename = savePath;
            string Level = (Convert.ToInt32(objElement.level) - 1).ToString();

            XmlDocument doc = new XmlDocument();
            if (global_Session._global_instance.xmlDocument != null)
            {
                doc = global_Session._global_instance.xmlDocument;
            }
            //doc.Load(filename);
            //select your specific node ..
            XmlElement oldNode = null;
            XmlElement oldNode1 = null;
            if (CaptionValue == "")
            {
                oldNode = (XmlElement)doc.SelectSingleNode("//Index[@Level='" + Level + "']/Indexes");
                oldNode1 = (XmlElement)doc.SelectSingleNode("//Index[@Level='" + Level + "']/Indexes").LastChild;
            }
            else
            {
                oldNode = (XmlElement)doc.SelectSingleNode("//Index[@Level='" + Level + "'][Caption='" + CaptionValue + "']//Indexes");
                oldNode1 = (XmlElement)doc.SelectSingleNode("//Index[@Level='" + Level + "'][Caption='" + CaptionValue + "']//Indexes").LastChild;

            }

            string sortOrder = oldNode1.InnerXml;
            string regSortOrder = Regex.Match(sortOrder, "<SortOrder>(.*?)</SortOrder>", RegexOptions.IgnoreCase).Groups[1].Value;
            int orderNumber = Convert.ToInt32(regSortOrder.Trim()) + 1;


            objElement.sortorder = orderNumber.ToString().Trim();
            //create new node and add value
            XmlElement newNode = IndexesElementUpdateIndex(doc, objElement);
            oldNode.AppendChild(newNode);
            // Save to the XML file
            // doc.Save(savePath);
            global_Session._global_instance.xmlDocument = doc;
            //doc.Save(savePath);
        }


        static XmlElement CreateXmlNewNode(secondary_xml_data_model objXmlData)
        {
            XmlDocument objXmlDoc = new XmlDocument();
            XmlElement indexesNode1 = null;

            if (objXmlData.level_of_data == "1")
            {
                indexesNode1 = IndexesElementWithContent(objXmlDoc, objXmlData.level1);

            }
            else if (objXmlData.level_of_data == "2")
            {
                XmlElement indexesNodeContent2 = IndexesElementWithContent(objXmlDoc, objXmlData.level2);
                indexesNode1 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent2, objXmlData.level1);


            }
            else if (objXmlData.level_of_data == "3")
            {
                XmlElement indexesNodeContent3 = IndexesElementWithContent(objXmlDoc, objXmlData.level3);
                XmlElement indexesNodeContent2 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent3, objXmlData.level2);
                indexesNode1 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent2, objXmlData.level1);

            }

            else if (objXmlData.level_of_data == "4")
            {
                XmlElement indexesNodeContent4 = IndexesElementWithContent(objXmlDoc, objXmlData.level4);
                XmlElement indexesNodeContent3 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent4, objXmlData.level3);
                XmlElement indexesNodeContent2 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent3, objXmlData.level2);
                indexesNode1 = IndexesElementWithOutContent(objXmlDoc, indexesNodeContent2, objXmlData.level1);

            }
            return indexesNode1;

        }

        static void UpdateXmlNewNode(XmlElement XmlDocNewNode, string LevelToUpdate, string CaptionValue = "")
        {
            string Level = (Convert.ToInt32(LevelToUpdate) - 1).ToString();
            XmlDocument doc = new XmlDocument();
            if (global_Session._global_instance.xmlDocument != null)
            {
                doc = global_Session._global_instance.xmlDocument;
            }
            //select your specific node ..
            XmlElement oldNode = null;
            XmlElement oldNode1 = null;
            if (CaptionValue == "")
            {
                oldNode = (XmlElement)doc.SelectSingleNode("//Index[@Level='" + Level + "']/Indexes");
                oldNode1 = (XmlElement)doc.SelectSingleNode("//Index[@Level='" + Level + "']/Indexes").LastChild;
            }
            else if (LevelToUpdate=="1")
            {
                oldNode = (XmlElement)doc.SelectSingleNode("//Indexes");
                oldNode1 = (XmlElement)doc.SelectSingleNode("//Index//Indexes").LastChild;

            }
            else
            {
                oldNode = (XmlElement)doc.SelectSingleNode("//Index[@Level='" + Level + "'][Caption='" + CaptionValue + "']//Indexes");
                oldNode1 = (XmlElement)doc.SelectSingleNode("//Index[@Level='" + Level + "'][Caption='" + CaptionValue + "']//Indexes").LastChild;
                 

            }

            XmlNode importNode = doc.ImportNode(XmlDocNewNode.SelectSingleNode("//Index"), true);

            oldNode.AppendChild(importNode);
            // Save to the XML file
            //doc.Save(savePath);
            global_Session._global_instance.xmlDocument = doc;
            //global_Session._global_instance.xmlDocument.Save(global_Session._global_instance.savepath);
        }


        static XmlElement IndexesElementWithOutContent(XmlDocument objXmlDoc, XmlElement IndexesElement, index_element_data_model objElement)
        {

            XmlElement indexesNode = objXmlDoc.CreateElement("Indexes");
            indexesNode.InnerText = "";
            if (objElement != null)
            {
                XmlElement IndexNode = objXmlDoc.CreateElement("Index");
                IndexNode.SetAttribute("Level", "" + objElement.level + "");
                IndexNode.SetAttribute("HasChildren", "" + objElement.haschildlevel + "");

                XmlElement CaptionNode = objXmlDoc.CreateElement("Caption");

                CaptionNode.InnerText = objElement.caption;

                XmlElement DescriptionNode = objXmlDoc.CreateElement("Description");
                DescriptionNode.InnerText = objElement.description;

                XmlElement SortOrderNode = objXmlDoc.CreateElement("SortOrder");
                SortOrderNode.InnerText = objElement.sortorder;

                XmlElement FastcaseIdNode = objXmlDoc.CreateElement("FastcaseId");
                FastcaseIdNode.InnerText = objElement.fastcaseid;

                IndexNode.AppendChild(CaptionNode);
                IndexNode.AppendChild(DescriptionNode);
                IndexNode.AppendChild(SortOrderNode);
                IndexNode.AppendChild(FastcaseIdNode);
                indexesNode.AppendChild(IndexNode);
                IndexNode.AppendChild(IndexesElement);
            }
            return indexesNode;
        }

        static XmlElement IndexesElementWithContent(XmlDocument objXmlDoc, index_element_data_model objElement)
        {

            XmlElement indexesNode = objXmlDoc.CreateElement("Indexes");
            indexesNode.InnerText = "";
            if (objElement != null)
            {
                XmlElement IndexNode = objXmlDoc.CreateElement("Index");
                IndexNode.SetAttribute("Level", "" + objElement.level + "");
                IndexNode.SetAttribute("HasChildren", "0");

                XmlElement CaptionNode = objXmlDoc.CreateElement("Caption");

                CaptionNode.InnerText = objElement.caption;

                XmlElement DescriptionNode = objXmlDoc.CreateElement("Description");
                DescriptionNode.InnerText = objElement.description;

                XmlElement SortOrderNode = objXmlDoc.CreateElement("SortOrder");
                SortOrderNode.InnerText = objElement.sortorder;

                XmlElement FastcaseIdNode = objXmlDoc.CreateElement("FastcaseId");
                FastcaseIdNode.InnerText = objElement.fastcaseid;

                XmlElement ContentNode = objXmlDoc.CreateElement("Content");
                ContentNode.InnerText = objElement.content;

                XmlElement ShortnameNode = objXmlDoc.CreateElement("ShortName");
                ShortnameNode.InnerText = objElement.shortname;

                XmlElement RevisionHistoryNode = objXmlDoc.CreateElement("RevisionHistory");
                RevisionHistoryNode.InnerText = objElement.revisionhistory;


                IndexNode.AppendChild(CaptionNode);
                IndexNode.AppendChild(DescriptionNode);
                IndexNode.AppendChild(SortOrderNode);
                IndexNode.AppendChild(ContentNode);
                IndexNode.AppendChild(ShortnameNode);
                IndexNode.AppendChild(RevisionHistoryNode);
                IndexNode.AppendChild(FastcaseIdNode);
                indexesNode.AppendChild(IndexNode);
            }
            return indexesNode;
        }

        static XmlElement IndexesElementUpdateIndex(XmlDocument objXmlDoc, index_element_data_model objElement)
        {

            XmlElement IndexNode = objXmlDoc.CreateElement("Index");
            IndexNode.InnerText = "";
            if (objElement != null)
            {
                IndexNode.SetAttribute("Level", "" + objElement.level + "");
                IndexNode.SetAttribute("HasChildren", "0");

                XmlElement CaptionNode = objXmlDoc.CreateElement("Caption");

                CaptionNode.InnerText = objElement.caption;

                XmlElement DescriptionNode = objXmlDoc.CreateElement("Description");
                DescriptionNode.InnerText = objElement.description;

                XmlElement SortOrderNode = objXmlDoc.CreateElement("SortOrder");
                SortOrderNode.InnerText = objElement.sortorder;

                XmlElement ContentNode = objXmlDoc.CreateElement("Content");
                ContentNode.InnerText = objElement.content;

                XmlElement FastcaseIdNode = objXmlDoc.CreateElement("FastcaseId");
                FastcaseIdNode.InnerText = objElement.fastcaseid;

                XmlElement ShortnameNode = objXmlDoc.CreateElement("ShortName");
                ShortnameNode.InnerText = objElement.shortname;

                XmlElement RevisionHistoryNode = objXmlDoc.CreateElement("RevisionHistory");
                RevisionHistoryNode.InnerText = objElement.revisionhistory;

                IndexNode.AppendChild(CaptionNode);
                IndexNode.AppendChild(DescriptionNode);
                IndexNode.AppendChild(SortOrderNode);
                IndexNode.AppendChild(ContentNode);
                IndexNode.AppendChild(ShortnameNode);
                IndexNode.AppendChild(RevisionHistoryNode);
                IndexNode.AppendChild(FastcaseIdNode);
            }
            return IndexNode;
        }

        static html_meta_model Get_AllMetaValue(string Content, Opinion OpinionData, string FilePath, string IncomingPath, string OutgoingPath, string ErrorLogFile)
        {
            html_meta_model ObjData = new html_meta_model();
            ObjData.Catchline = ReadMetaTag(Content, "Catchline");
            ObjData.CodeSec = ReadMetaTag(Content, "CodeSec");

            if (ObjData.CodeSec != null)
            {
                string codesecValue = ObjData.CodeSec;
                string sortValue = Regex.Match(codesecValue, "cbj\\s+\\d+", RegexOptions.IgnoreCase).Value;
                if (sortValue.Trim() == "")
                {
                    sortValue = "1";
                }
                else
                {
                    sortValue = sortValue.ToLower().Replace("cbj", "").Trim();
                }
                ObjData.SortOrderValue = sortValue;

            }

            ObjData.CodeTitle = ReadMetaTag(Content, "CodeTitle");
            ObjData.DataType = ReadMetaTag(Content, "DataType");
            ObjData.DocumentName = ReadMetaTag(Content, "DocumentName");
            ObjData.OpinionFullSearch = ReadMetaTag(Content, "OpinionFullSearch");
            ObjData.OpinionSearch = ReadMetaTag(Content, "OpinionSearch");
            ObjData.SessionYr = ReadMetaTag(Content, "SessionYr");
            ObjData.State = ReadMetaTag(Content, "State");
            ObjData.TitleSearch = ReadMetaTag(Content, "TitleSearch");
            ObjData.Version = ReadMetaTag(Content, "Version");
            ObjData.VolumeFullSearch = ReadMetaTag(Content, "VolumeFullSearch");
            ObjData.VolumeSearch = ReadMetaTag(Content, "VolumeSearch");
            ObjData.OpinionData = OpinionData;
            ObjData.File = FilePath;
            ObjData.IncomingPath = IncomingPath;
            ObjData.OutputPath = OutgoingPath;
            ObjData.ErrorLogFile = ErrorLogFile;


            return ObjData;
        }

    }
    public class Opinion
    {
        public string Browslevel1 { get; set; }
        public string Browslevel2 { get; set; }
        public string Browslevel3 { get; set; }
        public string Browslevel4 { get; set; }

        public string VolumeName { get; set; }
        public string ShortName { get; set; }
        public List<Docket> DocketNumbers { get; set; }
        public string DecisionDate { get; set; }
        public string Author { get; set; }
        public string PartyHeader { get; set; }
        public string HeaderHtml { get; set; }
        public string OpinionHtml { get; set; }
        public string LibraryName { get; set; }
        public string CourtAbbreviation { get; set; }
        public string SaveFolderPath { get; set; }
        public string ProductionFolderPath { get; set; }

        public string FilePath { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
    }
    public class Docket
    {
        public string DocketNumber { get; set; }
        public Docket()
        { this.DocketNumber = string.Empty; }
        public Docket(string DocketNumber)
        {
            this.DocketNumber = DocketNumber;
        }
    }
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }


    // created by santosh 
    public class HeaderOpinion
    {
        public string Browslevel1 { get; set; }

        public string Browslevel2 { get; set; }

        public string Browslevel3 { get; set; }

        public string Browslevel4 { get; set; }

        public string VolumeName { get; set; }
        public string ShortName { get; set; }
        public string DocketNumber { get; set; }
        public string DecisionDate { get; set; }
        public string Author { get; set; }
        public string PartyHeader { get; set; }
        public string HeaderHtml { get; set; }
        public string OpinionHtml { get; set; }
        public string LibraryName { get; set; }
        public string CourtAbbreviation { get; set; }
        public string SaveFolderPath { get; set; }
        public string ProductionFolderPath { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }



    }

    public class index_element_data_model
    {
        public string index { get; set; }
        public string caption { get; set; } = "";
        public string description { get; set; } = "";
        public string sortorder { get; set; } = "";
        public string fastcaseid { get; set; } = Guid.NewGuid().ToString();
        public string content { get; set; } = "";
        public string shortname { get; set; } = "";
        public string revisionhistory { get; set; } = "";

        public string level { get; set; } = "1";
        public string haschildlevel { get; set; } = "0";

    }


    public class secondary_xml_data_model
    {
        public string level_of_data { get; set; } = "2";
        public index_element_data_model level1 { get; set; }

        public index_element_data_model level2 { get; set; }

        public index_element_data_model level3 { get; set; }

        public index_element_data_model level4 { get; set; }

        public string edition_date { get; set; } = "";
        public string revision_date { get; set; } = "";
        public string currency_text { get; set; } = "";
        public string library_edition_description { get; set; } = "";
        public string library_SourceConst { get; set; } = "";
        public string library_name { get; set; } = "";



    }



    public class level1_data_model
    {

        public List<html_meta_model> browerdata { get; set; }

    }

    public class html_meta_model
    {
        public string State { get; set; }
        public string DataType { get; set; }
        public string SessionYr { get; set; }
        public string CodeTitle { get; set; }
        public string CodeSec { get; set; }

        public string Catchline { get; set; }
        public string Version { get; set; }
        public string DocumentName { get; set; }
        public string OpinionSearch { get; set; }
        public string OpinionFullSearch { get; set; }
        public string VolumeSearch { get; set; }
        public string VolumeFullSearch { get; set; }
        public string TitleSearch { get; set; }

        public string SortOrderValue { get; set; }

        public Opinion OpinionData { get; set; }

        public string File { get; set; }
        public string IncomingPath { get; set; }
        public string OutputPath { get; set; }
        public string ErrorLogFile { get; set; }

    }



}
