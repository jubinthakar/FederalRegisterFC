using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Xml;
namespace ConvertSecondaryToXML.Model
{
    public class data_model
    {
        string testStrData = "";
        string ConnDB = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
        public List<federal_register_model> GetAllDataFromDatabase(string year, string month)
        {
            List<federal_register_model> allData = new List<federal_register_model>();
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader Dr = null;
            string sqlQuery = "SELECT  id, iYear, iMonth, sMonth, iDate, agencyName, frDocket, frCitation, frPage, cfrCitation, issueNo, filepath, isactive, startpageNo, endpageNo " +
                "FROM federalregister " +
                "WHERE (iYear = '" + year + "') AND (iMonth = '" + month + "') AND isactive='True' " +
                "ORDER BY frPage DESC";
            try
            {
                conn = new SqlConnection(ConnDB);
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                cmd = new SqlCommand(sqlQuery, conn);
                Dr = cmd.ExecuteReader();

                if (Dr.HasRows)
                {
                    while (Dr.Read())
                    {
                        federal_register_model objData = new federal_register_model();
                        objData.id = Convert.ToInt32(Dr["id"]);
                        objData.iYear = Convert.ToString(Dr["iYear"]);
                        objData.iMonth = Convert.ToString(Dr["iMonth"]);
                        objData.sMonth = Convert.ToString(Dr["sMonth"]);
                        objData.iDate = Convert.ToDateTime(Dr["iDate"]);
                        objData.agencyName = Convert.ToString(Dr["agencyName"]);
                        objData.frDocket = Convert.ToString(Dr["frDocket"]);
                        objData.frCitation = Convert.ToString(Dr["frCitation"]);
                        objData.frPage = Convert.ToString(Dr["frPage"]);
                        
                        objData.cfrCitation = Convert.ToString(Dr["cfrCitation"]);
                        objData.issueNo = Convert.ToString(Dr["issueNo"]);
                        objData.filepath = Convert.ToString(Dr["filepath"]);
                        objData.isactive = Convert.ToString(Dr["isactive"]);
                        objData.startpageNo = Convert.ToString(Dr["startpageNo"]);
                        objData.endpageNo = Convert.ToString(Dr["endpageNo"]);
                        //if (allData.Count<5)
                        //{

                        if (objData.frPage != "" && objData.frPage != null)
                        {
                            allData.Add(objData);
                        }

                        //}

                    }
                }
            }
            catch (Exception ex)
            {


            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    conn.Dispose();
                }

            }


            return allData;
        }

        public List<federal_date_data_model> GetAllDataDatewise(List<federal_register_model> allFederalData)
        {
            List<federal_date_data_model> allData = new List<federal_date_data_model>();
            List<DateTime> allDates = allFederalData.Select(O => O.iDate).Distinct().ToList();
            foreach (DateTime Datedata in allDates)
            {
                List<federal_register_model> AllMonthFederal = allFederalData.Where(O => O.iDate == Datedata).ToList()
                    .OrderBy(O => Convert.ToInt32(O.frPage)).ToList();
                federal_register_model firstObject = AllMonthFederal.FirstOrDefault();
                federal_register_model lastObject = AllMonthFederal.LastOrDefault();

                federal_date_data_model objDateFederal = new federal_date_data_model();

                // string shortname= Datedata.ToString("MMMM")+ " "+ Datedata.ToString("dd") + ", "+
                //      Datedata.ToString("yyyy") + " ("+firstObject.issueNo.Trim()+") (Pages "+firstObject.frPage+" - "+ lastObject.frPage+ ")";

                string shortname = Datedata.ToString("MMMM d, yyyy") + " (" + firstObject.issueNo.Trim() + ") (Pages " + firstObject.frPage + " - " + lastObject.frPage + ")";

                objDateFederal.short_name = shortname;
                objDateFederal.date_time = Datedata;
                objDateFederal.start_page = firstObject.frPage;
                objDateFederal.end_page = lastObject.frPage;
                objDateFederal.allFederalData = AllMonthFederal;
                allData.Add(objDateFederal);
            }
            return allData;
        }
        public List<federal_date_data_model> GetAllFile(string path, string ErrorLogPath, string startYear, string endYear)
        {
            List<federal_date_data_model> allData = new List<federal_date_data_model>();
            for (int i = Convert.ToInt32(startYear); i <= Convert.ToInt32(endYear); i++)
            {
                for (int j = 1; j <= 1; j++)
                {

                    string Year = Convert.ToString(i);
                    string Month = Convert.ToString(j);
                    string MainPath = "";
                    if (path.IndexOf("Federal Register") > 0)
                    {
                        MainPath = path.Substring(0, path.IndexOf("Federal Register")) + "Federal Register";
                    }
                    else {
                        MainPath = path;

                    }
                    List<federal_register_model> allFederalData = GetAllDataFromDatabase(Year, Month);

                    List<federal_date_data_model> allFederalData2 = GetAllDataDatewise(allFederalData);

                    foreach (federal_date_data_model item in allFederalData2)
                    {
                        allData.Add(item);
                    }


                    //foreach (federal_register_model item in allFederalData)
                    //{
                    //    if (item.filepath != null)
                    //    {
                    //        if (item.filepath.IndexOf("FederalRegister") > 0)
                    //        {
                    //            string filePath = item.filepath.Substring(item.filepath.IndexOf("FederalRegister") + ("FederalRegister").Length);
                    //            string fullPath = MainPath + filePath;
                    //            if (File.Exists(fullPath))
                    //            {
                    //                allData.Add(fullPath);
                    //            }
                    //            else
                    //            {
                    //                File.AppendAllText(ErrorLogPath + "\\filenotfound.txt", "File Not Found in Physical Folder=" + fullPath + "\n");
                    //            }
                    //        }
                    //        else
                    //        {
                    //            File.AppendAllText(ErrorLogPath + "\\filenotfound.txt", "File Different in database=" + item.filepath + "\n");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        string abc = "";
                    //    }


                    //}
                }
            }

            allData = allData.OrderBy(O => O.date_time).ToList();

            return allData;
        }

    }



    public class federal_register_model
    {

        public int id { get; set; }
        public string iYear { get; set; }
        public string iMonth { get; set; }
        public string sMonth { get; set; }
        public DateTime iDate { get; set; }
        public string agencyName { get; set; }
        public string frDocket { get; set; }
        public string frCitation { get; set; }
        public string frPage { get; set; }
        public string cfrCitation { get; set; }
        public string issueNo { get; set; }
        public string filepath { get; set; }
        public string isactive { get; set; }
        public string startpageNo { get; set; }
        public string endpageNo { get; set; }

    }

    public class federal_date_data_model
    {
        public DateTime date_time { get; set; }
        public string start_page { get; set; }
        public string end_page { get; set; }

        public string short_name { get; set; }
        public List<federal_register_model> allFederalData { get; set; }
    }

    public sealed class global_Session
    {

        public static readonly global_Session _global_instance = new global_Session();
        static global_Session() { }
        private global_Session() { }
        public static global_Session OnceCreateInstance
        {
            get
            {
                return _global_instance;
            }

        }

        public XmlDocument xmlDocument { get; set; }

        public string savepath { get; set; }

        public string BrowseLevel1 { get; set; }
        public string BrowseLevel2 { get; set; }
        public string BrowseLevel3 { get; set; }
        public string BrowseLevel4 { get; set; }



    }

    public class filedata_model
    {

        public string filepath { get; set; }

        public List<federal_date_data_model> allFedData { get; set; }

    }
}
