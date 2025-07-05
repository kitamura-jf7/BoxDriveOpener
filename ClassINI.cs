using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;

namespace MYINI
{
    class ClassINI
    {
        [DllImport("KERNEL32.DLL")]
        private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
        [DllImport("KERNEL32.DLL")]
        public static extern uint WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        private string FFileName = "";
        private bool FUseOnMemory = false;
        private bool FUseAutoReload = true;
        private FileInfo FFileInfo = null;
        private List<string> FDatas = null;

        public string BaseFilename
        {
            get
            {
                return this.FFileName;
            }
        }

        public string Filename
        {
            get
            {
                return this.GetIniFilename();
            }
        }

        public ClassINI(string Filename, bool UseOnMemory, bool UseAutoReload)
        {
            this.FFileName = Filename;
            this.FUseOnMemory = UseOnMemory;
            this.FUseAutoReload = UseAutoReload;
        }
        public ClassINI(string Filename)
        {
            this.FFileName = Filename;
        }

        private string GetIniFilename()
        {
            FileInfo exe = new FileInfo(Application.ExecutablePath);
            if (this.FFileName == "")
            {
                //  FileName未指定の時は、EXEと同じフォルダのiniとする
                return exe.FullName.Replace(exe.Extension, ".ini");
            }
            else
            {
                if (this.FFileName.Contains(@"\") == false)
                {
                    //  FileNameにPATHを含んで無い時は、EXEと同じフォルダとする
                    return exe.DirectoryName + @"\" + this.FFileName;
                }
                else
                {
                    return this.FFileName;
                }
            }
        }

        public bool Read(string SectionName, string KeyName, bool DefaultValue)
        {
            try
            {
                string S = this.Read(SectionName, KeyName, DefaultValue.ToString());
                try
                {
                    return int.Parse(S) != 0;   //  0はfalse、以外はtrue
                }
                catch
                {
                    return bool.Parse(S);   //  数値以外
                }
            }
            catch
            {
                return DefaultValue;
            }
        }

        public DateTime Read(string SectionName, string KeyName, DateTime DefaultValue)
        {
            try
            {
                return DateTime.Parse(this.Read(SectionName, KeyName, DefaultValue.ToString()));
            }
            catch
            {
                return DefaultValue;
            }
        }

        public decimal Read(string SectionName, string KeyName, decimal DefaultValue)
        {
            try
            {
                return decimal.Parse(this.Read(SectionName, KeyName, DefaultValue.ToString()));
            }
            catch
            {
                return DefaultValue;
            }
        }

        public double Read(string SectionName, string KeyName, double DefaultValue)
        {
            try
            {
                return double.Parse(this.Read(SectionName, KeyName, DefaultValue.ToString()));
            }
            catch
            {
                return DefaultValue;
            }
        }

        public float Read(string SectionName, string KeyName, float DefaultValue)
        {
            try
            {
                return float.Parse(this.Read(SectionName, KeyName, DefaultValue.ToString()));
            }
            catch
            {
                return DefaultValue;
            }
        }

        public int Read(string SectionName, string KeyName, int DefaultValue)
        {
            try
            {
                return int.Parse(this.Read(SectionName, KeyName, DefaultValue.ToString()));
            }
            catch
            {
                return DefaultValue;
            }
        }

        public string Read(string SectionName, string KeyName, string DefaultValue)
        {
            string S = this.GetIniFilename();
            if (this.FUseOnMemory == false)
            {
                StringBuilder sb = new StringBuilder(65536);
                if (GetPrivateProfileString(SectionName, KeyName, "", sb, (uint)sb.Capacity, S) > 0)
                {
                    return sb.ToString();
                }
                else
                {
                    return DefaultValue;
                }
            }
            else
            {
                if (this.FFileInfo == null 
                    || this.FUseAutoReload == true && this.FFileInfo.LastWriteTime.CompareTo(new FileInfo(S).LastWriteTime) < 0)
                {
                    this.FFileInfo = new FileInfo(S);
                    if (this.FFileInfo.Exists == false)
                    {
                        this.FDatas = null;
                    }
                    else
                    {
                        //  iniファイル全体の内容をメモリにロード
                        this.FDatas = new List<string>();
                        StreamReader SR = new StreamReader(S, Encoding.Default);
                        try
                        {
                            while (SR.Peek() != -1)
                            {
                                this.FDatas.Add(SR.ReadLine());
                            }
                        }
                        finally
                        {
                            SR.Close();
                        }
                    }
                }
                if (this.FDatas != null)
                {
                    bool WFindSection = false;
                    foreach (string DT in this.FDatas)
                    {
                        if (WFindSection == false)
                        {
                            if (DT.StartsWith("[" + SectionName + "]") == true)
                            {
                                //  目的のセクションが見つかった
                                WFindSection = true;
                            }
                        }
                        else
                        {
                            if (DT.StartsWith("[") == true)
                            {
                                //  次のセクションが見つかった
                                return DefaultValue;
                            }
                            else
                            {
                                if (DT.StartsWith(KeyName + "=") == true)
                                {
                                    //  目的のキーが見つかった
                                    return DT.Substring(KeyName.Length + 1);
                                }
                            }
                        }
                    }
                }
                return DefaultValue;
            }
        }

        public string[] Read(string SectionName, string KeyName, string[] DefaultValue)
        {
            ArrayList RR = new ArrayList();
            int n = this.Read(SectionName, KeyName + ".COUNT", -1); //  行数取得
            if (n == -1)
            {
                return DefaultValue;
            }
            else
            {
                for (int i = 1; i <= n; i++)
                {
                    string S = this.Read(SectionName, KeyName + "." + i.ToString(), "");
                    RR.Add(S);
                }
                return (string[])RR.ToArray(typeof(string));
            }
        }

        public Dictionary<string, string> Read(string SectionName, string KeyName, Dictionary<string, string> DefaultValue)
        {
            Dictionary<string, string> RR = new Dictionary<string, string>();
            int n = this.Read(SectionName, KeyName + ".COUNT", -1); //  行数取得
            if (n == -1)
            {
                return DefaultValue;
            }
            else
            {
                for (int i = 1; i <= n; i++)
                {
                    string S1 = this.Read(SectionName, KeyName + "." + i.ToString() + ".KEY", "");
                    string S2 = this.Read(SectionName, KeyName + "." + i.ToString() + ".VAL", "");
                    RR[S1] = S2;
                }
                return RR;
            }
        }

        public System.Drawing.Point Read(string SectionName, string KeyName, System.Drawing.Point DefaultValue)
        {
            try
            {
                return new System.Drawing.Point(this.Read(SectionName, KeyName + ".X", DefaultValue.X), this.Read(SectionName, KeyName + ".Y", DefaultValue.Y));
            }
            catch
            {
                return DefaultValue;
            }
        }

        public System.Drawing.Size Read(string SectionName, string KeyName, System.Drawing.Size DefaultValue)
        {
            try
            {
                return new System.Drawing.Size(this.Read(SectionName, KeyName + ".Width", DefaultValue.Width), this.Read(SectionName, KeyName + ".Height", DefaultValue.Height));
            }
            catch
            {
                return DefaultValue;
            }
        }

        public System.Drawing.Rectangle Read(string SectionName, string KeyName, System.Drawing.Rectangle DefaultValue)
        {
            try
            {
                return new System.Drawing.Rectangle(this.Read(SectionName, KeyName, DefaultValue.Location), this.Read(SectionName, KeyName, DefaultValue.Size));
            }
            catch
            {
                return DefaultValue;
            }
        }

        public void Write(string SectionName, string KeyName, bool Value)
        {
            this.Write(SectionName, KeyName, Value.ToString());
        }

        public void Write(string SectionName, string KeyName, DateTime Value)
        {
            this.Write(SectionName, KeyName, Value.ToString("yyyy/MM/dd,HH:mm:ss") + "." + Value.Millisecond.ToString("000"));
        }

        public void Write(string SectionName, string KeyName, decimal Value)
        {
            this.Write(SectionName, KeyName, Value.ToString());
        }

        public void Write(string SectionName, string KeyName, double Value)
        {
            this.Write(SectionName, KeyName, Value.ToString());
        }

        public void Write(string SectionName, string KeyName, float Value)
        {
            this.Write(SectionName, KeyName, Value.ToString());
        }

        public void Write(string SectionName, string KeyName, int Value)
        {
            this.Write(SectionName, KeyName, Value.ToString());
        }

        public void Write(string SectionName, string KeyName, string Value)
        {
            WritePrivateProfileString(SectionName, KeyName, Value, this.GetIniFilename());
        }

        public void Write(string SectionName, string KeyName, string[] Value)
        {
            for (int i = 1; i <= Value.Length; i++)
            {
                this.Write(SectionName, KeyName + "." + i.ToString(), Value[i - 1]);
            }
            this.Write(SectionName, KeyName + ".COUNT", Value.Length);  //  行数
            //  不要になった余分な明細の削除は放置
        }

        public void Write(string SectionName, string KeyName, Dictionary<string, string> Value)
        {
            this.Write(SectionName, KeyName + ".COUNT", Value.Count);  //  行数
            int i = 0;
            {
                foreach (KeyValuePair<string, string> value in Value)
                {
                    i++;
                    this.Write(SectionName, KeyName + "." + i.ToString() + ".KEY", value.Key);
                    this.Write(SectionName, KeyName + "." + i.ToString() + ".VAL", value.Value);
                }
            }
            //  不要になった余分な明細の削除は放置
        }

        public void Write(string SectionName, string KeyName, System.Drawing.Point Value)
        {
            this.Write(SectionName, KeyName + ".X", Value.X);
            this.Write(SectionName, KeyName + ".Y", Value.Y);
        }

        public void Write(string SectionName, string KeyName, System.Drawing.Size Value)
        {
            this.Write(SectionName, KeyName + ".Width", Value.Width);
            this.Write(SectionName, KeyName + ".Height", Value.Height);
        }

        public void Write(string SectionName, string KeyName, System.Drawing.Rectangle Value)
        {
            this.Write(SectionName, KeyName, Value.Location);
            this.Write(SectionName, KeyName, Value.Size);
        }

        public void Delete(string SectionName, string KeyName)
        {
            WritePrivateProfileString(SectionName, KeyName, null, this.GetIniFilename());
        }

        public void Delete(string SectionName)
        {
            WritePrivateProfileString(SectionName, null, null, this.GetIniFilename());
        }

    }
}
