using System.IO;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text.RegularExpressions;

namespace VMPoolTest_Arthrex.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VMPoolController : ControllerBase
    {
        private static string strFilePath = Path.Combine(Environment.CurrentDirectory, "VMList.csv");
        private static DateTime time = new DateTime();
        private static Regex regex = new Regex(@"^[a-zA-Z0-9]+$");

        /*
         * GET Request
         * Update VM details: From Free to Occupied Status
         * Record VM Checkout Time
         */

        [HttpGet("Checkout/{username}")]
        public ActionResult Checkout(string username)
        {
            DataTable dt = readVMList();
            string? ipaddress = null;

            try
            {
                
                if (regex.IsMatch(username))
                {
                    var dataRow = dt.AsEnumerable().Where(x => x.Field<string>("status") == "occupied"
                                                            && x.Field<string>("username") == username);

                    if (dataRow.Count() > 0)
                        return StatusCode(400, "Sorry: "+ username +" : Already Checked-out with VM: "+ dataRow.First()["ipaddress"]);
                   
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["status"].Equals("free") && !dr["username"].Equals(username))
                        {
                            updateVMCSV(dt, dr, username, "occupied");
                            ipaddress = (string)dr["ipaddress"];
                            break;
                        }
                    }
                }
                else
                    return StatusCode(400, "Sorry: No Authorization: Invalid Username");

                if (!String.IsNullOrEmpty(ipaddress))
                    return StatusCode(200, "User: " + username + " :Checked-Out VM: " + ipaddress + " : At time: " + DateTime.UtcNow);
                else
                    return StatusCode(400, "Sorry: No VM is available - Please retry after some time");
            }
            catch (Exception e)
            { return StatusCode(400, e.StackTrace); }
        }

        /*
         * POST Request
         * Clean Up VM Details: From Occupied to Free Status
         * Calulate VM Usage Time
         */

        [HttpPost("Checkin/{username}")]
        public ActionResult checkin(string username)
        {
            DataTable dt = readVMList();
            string? ipaddress = null;

            try
            {
                if (regex.IsMatch(username))
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["status"].Equals("occupied") && dr["username"].Equals(username))
                        {
                            time = updateVMCSV(dt, dr, "", "free");
                            ipaddress = (string)dr["ipaddress"];
                            break;
                        }
                    }
                }
                else
                    return StatusCode(400, "Sorry: No Authorization: Invalid Username");

                if (!String.IsNullOrEmpty(ipaddress))
                    return StatusCode(201, "User: " + username + " :checked-in from VM: " + ipaddress + " :Total Usage time in minutes: " + Math.Round((DateTime.UtcNow.Subtract(time)).TotalMinutes,2));
                else
                    return StatusCode(400, "Sorry: you're not checked-out in any of the VM");
            }
            catch(Exception e)
            { return StatusCode(400, e.StackTrace); }
        }


        /*
         * POST Request
         * Clean Up all VM Details: From Occupied to Free Status
         * Server Refresh
         */

        [HttpPost("ServerRefresh/{username}/{password}")]
        public ActionResult refreshServer(string username, string password)
        {
            DataTable dt = readVMList();

            try
            {
                if (username.Equals("admin") && password.Equals("123456"))
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (!dr["status"].Equals("free"))
                        {
                            dr["status"] = "free";
                            dr["username"] = "";
                            dr["time"] = "";

                            dt.AcceptChanges();
                        }
                    }
                    saveToCSV(dt);
                }
                else
                    return StatusCode(400, "Sorry: No Authorization: Invalid Admin Credentials");

                return StatusCode(201, "Server Refreshed Successfully");
            }
            catch (Exception e)
            { return StatusCode(400, e.StackTrace); }
        }

        /*
         * Function to Update VM details on Checkout request/Clean-up VM details on Checked-in in DataRow
         */
        private DateTime updateVMCSV(DataTable dt, DataRow dr, string username, string status)
        {
            dr["status"] = status;
            dr["username"] = username;
            time = status.Equals("occupied") ? DateTime.UtcNow : DateTime.Parse((string)dr["time"]);
            dr["time"] = DateTime.UtcNow.ToString();
         
            dt.AcceptChanges();

            saveToCSV(dt);
            return time;
        }

        /*
         * Function to read updated status of VM Pool from CSV
         */
        public static DataTable readVMList()
        {
            DataTable dt = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(strFilePath))
                {
                    string[] headers = sr.ReadLine().Split(',');
                    foreach (string header in headers)
                    {
                        dt.Columns.Add(header);
                    }
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i];
                        }
                        dt.Rows.Add(dr);
                    }
                }
            }
            catch (Exception e) { };
            return dt;
        }

        /*
         * Funtion to Save updated VM Status data to CSV
         */

        public static void saveToCSV(DataTable dt)
        {
            using (var writer = new StreamWriter(strFilePath))
            {
                writer.WriteLine(string.Join(",", dt.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName)));
                foreach (DataRow row in dt.Rows)
                {
                    writer.WriteLine(string.Join(",", row.ItemArray));
                }
            }

        }



    }
}