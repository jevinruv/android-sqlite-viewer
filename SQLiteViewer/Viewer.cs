using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Configuration;

namespace SQLiteViewer
{
    public partial class Viewer : Form
    {
        private string seperator = "|^*|";
        private DataTable dt;

        private string adbPath;
        private string packageName;
        private string databaseName;

        public Viewer()
        {
            InitializeComponent();
            dataGridView1.DataSource = dt;
            this.init();
        }

        private void init()
        {
            this.adbPath = ConfigurationManager.AppSettings.Get("adbPath");
            this.packageName = ConfigurationManager.AppSettings.Get("packageName");
            this.databaseName = ConfigurationManager.AppSettings.Get("databaseName");
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            string query = this.inputQuery.Text.Trim();
            this.runCommand(query);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            this.inputQuery.Clear();
            this.dt.Clear();
        }

        private void runCommand(string query)
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.WorkingDirectory = this.adbPath;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine("adb shell");
            cmd.StandardInput.WriteLine("cd data/data/{0}/databases", this.packageName);
            cmd.StandardInput.WriteLine("sqlite3 {0}", this.databaseName);
            cmd.StandardInput.WriteLine(".header on");
            cmd.StandardInput.WriteLine(".separator " + "'" + this.seperator + "'");
            cmd.StandardInput.WriteLine(query);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();

            //string output = cmd.StandardOutput.ReadToEnd();
            int lineCounter = 0;
            string line;
            bool areColumnsAdded = false;
            this.dt = new DataTable();

            while ((line = cmd.StandardOutput.ReadLine()) != null)
            {
                //while (!cmd.StandardOutput.EndOfStream)

                lineCounter++;
                if (lineCounter < 5) continue;
                //Console.WriteLine(line);

                if (string.IsNullOrEmpty(line)) break;

                if (areColumnsAdded)
                {
                    this.addDataToListView(line);
                }
                else
                {
                    this.addColumnsToListView(line);
                    areColumnsAdded = true;
                }
            }

            cmd.WaitForExit();

        }

        private void addColumnsToListView(string line)
        {
            string[] columns = line.Split(new string[] { seperator }, StringSplitOptions.None);

            //Console.WriteLine(line);

            foreach (string columnName in columns)
            {
                this.dt.Columns.Add(columnName);
            }

            dataGridView1.DataSource = dt;
        }

        private void addDataToListView(string line)
        {
            string[] data = line.Split(new string[] { seperator }, StringSplitOptions.None);

            //Console.WriteLine(line);

            DataRow dr = dt.NewRow();
            dr.ItemArray = data;
            dt.Rows.Add(dr);
        }

        private void inputQuery_TextChanged(object sender, EventArgs e)
        {
            string tokens = "(select|SELECT|WHERE|FROM)";

            Regex rex = new Regex(tokens);
            MatchCollection mc = rex.Matches(inputQuery.Text);
            int StartCursorPosition = inputQuery.SelectionStart;
            foreach (Match m in mc)
            {
                int startIndex = m.Index;
                int StopIndex = m.Length;
                inputQuery.Select(startIndex, StopIndex);
                inputQuery.SelectionColor = Color.Blue;
                inputQuery.SelectionStart = StartCursorPosition;
                inputQuery.SelectionColor = Color.Black;
            }
        }
    }
}
