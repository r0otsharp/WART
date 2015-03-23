using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using WART.AppCode;

namespace WART
{
    public partial class frmRegister : Form
    {
        protected string identity = string.Empty;
        protected string number = string.Empty;
        protected string cc = string.Empty;
        protected string phone = string.Empty;
        protected string password = string.Empty;
        protected string code = string.Empty;
        protected bool raw = false;
        public  string method = "sms";
        protected ToolTip tt;
        private string[] WaCertThumbprints = {
                                                 "AC4C5FDEAEDD00406AC33C58BAFD6DE6D2424FEE", 
                                                 "738F92D22B2A2E6A8A42C60964B93FCCB456957F",
                                                 "155906D29D14DF2A54F039F6E170C2A7F97EEDEE",
                                                 "F8E2555BB2D58A76995A6B897CEAFA032CFA6C27"
                                             };
        protected bool debug;

        public frmRegister()
        {
            InitializeComponent();
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.ServerCertificateValidationCallback += CustomCertificateValidation;
        }

        private bool CustomCertificateValidation(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            if(sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
            {
                if (!WaCertThumbprints.Contains(certificate.GetCertHashString()))
                    return this.AskCertificateApproval(sslPolicyErrors, certificate);
            }
            return true;
        }

        protected bool AskCertificateApproval(System.Net.Security.SslPolicyErrors sslPolicyErrors, System.Security.Cryptography.X509Certificates.X509Certificate certificate)
        {
            DialogResult res = MessageBox.Show(this,
                String.Format("Warning: server certificate cannot be verified as trusted (Errors: {0}). Continue?\r\n\r\n{1}", sslPolicyErrors.ToString(), certificate.ToString()), 
                "Certificate error",
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Exclamation);
            return res == System.Windows.Forms.DialogResult.Yes;
        }

        private void AddToolTips()
        {
            this.tt = new ToolTip();
            this.tt.AutoPopDelay = 5000;
            this.tt.InitialDelay = 0;
            this.tt.ReshowDelay = 0;
            this.tt.ShowAlways = true;
            this.tt.SetToolTip(this.txtPassword, "Optional personal password. Using your own personal password will greatly increase security.");
            this.tt.SetToolTip(this.txtPhoneNumber, "Your phone number including country code (no leading + or 0)");
            this.tt.SetToolTip(this.txtCode, "6-digit verifiction code you received by SMS or voice call");
            this.tt.SetToolTip(this.btnID, "Generate ID by number and password and copy it to clipboard");
            this.tt.SetToolTip(this.btnExist, "Only check the submitted registration details and retrieve a new password if they match");
            this.tt.SetToolTip(this.btnCodeRequest, "Request registration code. You will receive a new password instead if the submitted registration already exists");
            this.tt.SetToolTip(this.btnSkip, "Skip code request and go to step 2 when you've already received a valid confirmation code for that number.");
        }

        private void btnCodeRequest_Click(object sender, EventArgs e)
        {
            if (this.parseNumber())
            {
                //try sms
                this.method = "sms";
                string resp1 = string.Empty;
                string resp2 = string.Empty;
                if (!this._requestCode(out resp1))
                {
                    //try using voice
                    this.method = "voice";
                    if (!this._requestCode(out resp2))
                    {
                        this.Notify(string.Format(@"Could not request code using either sms or voice.
SMS:    {0}
Voice:  {1}", resp1, resp2));
                    }
                }
            }
        }

        private bool parseNumber()
        {
            this.debug = this.chkDebug.Checked;
            if (!String.IsNullOrEmpty(this.txtPhoneNumber.Text))
            {
                try
                {
                    this.number = this.txtPhoneNumber.Text;
                    this.TrimNumber();
                    WhatsAppApi.Parser.PhoneNumber phonenumber = new WhatsAppApi.Parser.PhoneNumber(this.number);
                    this.identity = WhatsAppApi.Register.WhatsRegisterV2.GenerateIdentity(phonenumber.Number, this.txtPassword.Text);
                    this.cc = phonenumber.CC;
                    this.phone = phonenumber.Number;

                    CountryHelper chelp = new CountryHelper();
                    string country = string.Empty;
                    if (!chelp.CheckFormat(this.cc, this.phone, out country))
                    {
                        string msg = string.Format("Provided number does not match any known patterns for {0}", country);
                        this.Notify(msg);
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    string msg = String.Format("Error: {0}", ex.Message);
                    this.Notify(msg);
                }
            }
            else
            {
                this.Notify("Please enter a phone number");
            }
            return false;
        }

        private bool _requestCode(out string response)
        {
            string request = null;

            bool requestResult = WhatsAppApi.Register.WhatsRegisterV2.RequestCode(this.number, out this.password, out request, out response, this.method, this.identity);

            if (this.debug)
            {
                this.Notify(string.Format(@"Code Request:
Token = {0}
Identity = {1}
User Agent = {2}
Request = {3}
Response = {4}", WhatsAppApi.Register.WhatsRegisterV2.GetToken(this.phone), this.identity, WhatsAppApi.Settings.WhatsConstants.UserAgent, request, response));
            }

            if (requestResult)
            {
                if (!string.IsNullOrEmpty(this.password))
                {
                    //password received
                    this.OnReceivePassword();
                }
                else
                {
                    this.grpStep1.Enabled = false;
                    this.grpStep2.Enabled = true;
                    this.Notify(string.Format("Code sent by {0} to {1}", this.method, this.number));
                }
            }

            return requestResult;
        }

        private void btnRegisterCode_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.txtCode.Text) && this.txtCode.Text.Length == 6)
            {
                this.code = this.txtCode.Text;
                string response = string.Empty;
                this.password = WhatsAppApi.Register.WhatsRegisterV2.RegisterCode(this.number, this.code, out response, this.identity);
                if (this.debug)
                {
                    this.Notify(string.Format(@"Code register:
Code = {0}
Number = {1}
Identity = {2}
Response = {3}", this.code, this.number, this.identity, response));
                }
                if (!String.IsNullOrEmpty(this.password))
                {
                    this.OnReceivePassword();
                }
                else
                {
                    string msg = "Verification code not accepted";
                    this.Notify(msg);
                }
            }
        }

        private void Notify(string msg)
        {
            this.txtOutput.Text = msg;
            MessageBox.Show(msg);
        }

        private void OnReceivePassword()
        {
            this.Notify(String.Format("Got password:\r\n{0}\r\n\r\nWrite it down and exit the program", this.password));
            this.grpStep1.Enabled = false;
            this.grpStep2.Enabled = false;
            this.grpResult.Enabled = true;
        }

        private void frmRegister_Load(object sender, EventArgs e)
        {
            this.AddToolTips();
        }

        private void onMouseEnter(object sender, EventArgs e)
        {
            this.tt.Active = true;
        }

        private void onMouseLeave(object sender, EventArgs e)
        {
            this.tt.Active = false;
        }

        private void txtCode_TextChanged(object sender, EventArgs e)
        {
            if (this.txtCode.Text.Length == 6)
            {
                this.btnRegisterCode.Enabled = true;
            }
            else
            {
                this.btnRegisterCode.Enabled = false;
            }
        }

        private void btnID_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.txtPhoneNumber.Text))
            {
                try
                {
                    WhatsAppApi.Parser.PhoneNumber phonenumber = new WhatsAppApi.Parser.PhoneNumber(this.txtPhoneNumber.Text);
                    this.identity = WhatsAppApi.Register.WhatsRegisterV2.GenerateIdentity(phonenumber.Number, this.txtPassword.Text);
                    this.txtOutput.Text = String.Format("Your identity is copied to clipboard:\r\n{0}", this.identity);
                    Clipboard.SetText(this.identity);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void txtPhoneNumber_TextChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.txtPhoneNumber.Text))
            {
                this.btnID.Enabled = true;
                this.btnSkip.Enabled = true;
                this.btnCodeRequest.Enabled = true;
                this.btnExist.Enabled = true;
            }
            else
            {
                this.btnID.Enabled = false;
                this.btnSkip.Enabled = false;
                this.btnCodeRequest.Enabled = false;
                this.btnExist.Enabled = false;
            }
        }

        protected void TrimNumber()
        {
            this.number = this.number.TrimStart(new char[] { '+', '0' });
        }

        /*
         * command line mode:
         */

        public void RunAsCli()
        {
            string[] args = Environment.GetCommandLineArgs();
            string option = string.Empty;
            if (args.Length >= 2)
            {
                option = args[1];
            }
            switch (option)
            {
                case "id":
                    this.CliGenerateId();
                    break;
                case "request":
                    this.CliRequestCode();
                    break;
                case "register":
                    this.CliRegisterCode();
                    break;
                case "exist":
                    this.CliExist();
                    break;
                default:
                    //show tip
                    this.CliPrintHelp();
                    break;
            }
        }

        private void CliPrintHelp()
        {
            //print help
            Console.WriteLine();
            Console.WriteLine(String.Format("WART {0} - https://github.com/shirioko/WART", Assembly.GetExecutingAssembly().GetName().Version));
            Console.WriteLine("Created by:");
            Console.WriteLine("\tDynogic  - https://github.com/dynogic");
            Console.WriteLine("\tpastoso  - https://github.com/pastoso");
            Console.WriteLine("\tshirioko - https://github.com/shirioko");
            Console.WriteLine();
            Console.WriteLine("Usage: WART.exe [method] [args (key=value)]");
            Console.WriteLine();
            Console.WriteLine("Methods:");
            Console.WriteLine("\tui --- Forces WART to run as UI instead of CLI");
            Console.WriteLine("\tid number password --- Generates and prints identity");
            Console.WriteLine("\trequest number password method --- Requests registration code or gets password");
            Console.WriteLine("\tregister number password code --- Registers a number");
            Console.WriteLine("\texist number password --- Check an existing registration and retrieve a new password");
            Console.WriteLine();
            Console.WriteLine("Args:");
            Console.WriteLine("\tnumber --- Phone number incl. country code");
            Console.WriteLine("\tpassword (optional) --- Optional personal password for generation identity");
            Console.WriteLine("\tcode --- 6-digit registration code you received from whatsapp");
            Console.WriteLine("\tmethod (optional) --- Method for code delivery (sms/voice)");
            Console.WriteLine("\traw (optional) --- Return raw server response (true/false)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("\tWART.exe id number=1234567890 password=secret");
            Console.WriteLine("\tWART.exe request number=1234567890 password=secret method=sms");
            Console.WriteLine("\tWART.exe register number=1234567890 password=secret code=000000");
            Console.WriteLine("\tWART.exe exist number=1234567890 password=secret");
        }

        private void CliRegisterCode()
        {
            this.GetArgs();
            this.TrimNumber();
            try
            {
                WhatsAppApi.Parser.PhoneNumber pn = new WhatsAppApi.Parser.PhoneNumber(this.number);
                this.identity = WhatsAppApi.Register.WhatsRegisterV2.GenerateIdentity(pn.Number, password);
                CountryHelper ch = new CountryHelper();
                string country = string.Empty;
                if (ch.CheckFormat(pn.CC, pn.Number, out country))
                {
                    string response = string.Empty;
                    this.password = WhatsAppApi.Register.WhatsRegisterV2.RegisterCode(this.number, this.code, out response, this.identity);
                    
                    //return raw
                    if (this.raw)
                    {
                        Console.WriteLine(response);
                        return;
                    }

                    if (String.IsNullOrEmpty(this.password))
                    {
                        Console.WriteLine("Code not accepted");
                    }
                    else
                    {
                        Console.WriteLine("Got password:");
                        Console.WriteLine(this.password);
                    }
                }
                else
                {
                    Console.WriteLine(String.Format("Invalid number for {0}", country));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Error: {0}", e.Message));
            }
        }

        private void CliRequestCode()
        {
            this.GetArgs();
            this.TrimNumber();
            try
            {
                WhatsAppApi.Parser.PhoneNumber pn = new WhatsAppApi.Parser.PhoneNumber(this.number);
                this.identity = WhatsAppApi.Register.WhatsRegisterV2.GenerateIdentity(pn.Number, this.password);
                CountryHelper ch = new CountryHelper();
                string country = string.Empty;
                string response = string.Empty;
                if (ch.CheckFormat(pn.CC, pn.Number, out country))
                {
                    bool result = WhatsAppApi.Register.WhatsRegisterV2.RequestCode(this.number, out this.password, out response, this.method, this.identity);
                    
                    //return raw
                    if (this.raw)
                    {
                        Console.WriteLine(response);
                        return;
                    }

                    if (result)
                    {
                        if (!string.IsNullOrEmpty(this.password))
                        {
                            Console.WriteLine("Got password:");
                            Console.WriteLine(this.password);
                        }
                        else
                        {
                            Console.WriteLine("Code requested");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error:");
                        Console.WriteLine(response);
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("Invalid phone number for {0}", country));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void CliExist()
        {
            this.GetArgs();
            this.TrimNumber();
            try
            {
                WhatsAppApi.Parser.PhoneNumber pn = new WhatsAppApi.Parser.PhoneNumber(this.number);
                this.identity = WhatsAppApi.Register.WhatsRegisterV2.GenerateIdentity(pn.Number, this.password);
                CountryHelper ch = new CountryHelper();
                string country = string.Empty;
                string response = string.Empty;
                if (ch.CheckFormat(pn.CC, pn.Number, out country))
                {
                    this.password = WhatsAppApi.Register.WhatsRegisterV2.RequestExist(this.number, out response, this.identity);

                    //return raw
                    if (this.raw)
                    {
                        Console.WriteLine(response);
                        return;
                    }

                    if (!string.IsNullOrEmpty(this.password))
                    {
                        Console.WriteLine("Got password:");
                        Console.WriteLine(this.password);
                    }
                    else
                    {
                        Console.WriteLine("Error:");
                        Console.WriteLine(response);
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("Invalid phone number for {0}", country));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void CliGenerateId()
        {
            this.GetArgs();
            this.TrimNumber();
            try
            {
                WhatsAppApi.Parser.PhoneNumber pn = new WhatsAppApi.Parser.PhoneNumber(this.number);
                this.identity = WhatsAppApi.Register.WhatsRegisterV2.GenerateIdentity(pn.Number, this.password);
                if (!this.raw)
                {
                    Console.WriteLine("Identity:");
                }
                Console.WriteLine(identity);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void GetArgs()
        {
 	        foreach(string arg in Environment.GetCommandLineArgs())
            {
                if(arg.Contains('='))
                {
                    string[] parts = arg.Split(new char[] { '=' } );
                    try
                    {
                        switch (parts[0])
                        {
                            case "number":
                                this.number = parts[1];
                                break;
                            case "password":
                                this.password = parts[1];
                                break;
                            case "method":
                                this.method = parts[1];
                                break;
                            case "code":
                                this.code = parts[1];
                                break;
                            case "raw":
                                if (parts[1] == "true")
                                {
                                    this.raw = true;
                                }
                                break;
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private void btnExist_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(this.txtPhoneNumber.Text))
            {
                try
                {
                    this.number = this.txtPhoneNumber.Text;
                    this.TrimNumber();
                    WhatsAppApi.Parser.PhoneNumber phonenumber = new WhatsAppApi.Parser.PhoneNumber(this.number);
                    this.identity = WhatsAppApi.Register.WhatsRegisterV2.GenerateIdentity(phonenumber.Number, this.txtPassword.Text);
                    this.cc = phonenumber.CC;
                    this.phone = phonenumber.Number;

                    CountryHelper chelp = new CountryHelper();
                    string country = string.Empty;
                    if (!chelp.CheckFormat(this.cc, this.phone, out country))
                    {
                        string msg = string.Format("Provided number does not match any known patterns for {0}", country);
                        this.Notify(msg);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    string msg = String.Format("Error: {0}", ex.Message);
                    this.Notify(msg);
                    return;
                }
                string response = null;
                this.password = WhatsAppApi.Register.WhatsRegisterV2.RequestExist(this.number, out response, this.identity);

                if (!string.IsNullOrEmpty(this.password))
                {
                    //password received
                    this.OnReceivePassword();
                }
                else
                {
                    string msg = string.Format("Could not verify existing registration\r\n{0}", response);
                    this.Notify(msg);
                }
            }
            else
            {
                this.Notify("Please enter a phone number");
            }
        }

        private void btnSkip_Click(object sender, EventArgs e)
        {
            if (this.parseNumber())
            {
                this.grpStep1.Enabled = false;
                this.grpStep2.Enabled = true;
            }
        }
    }
}