using Google.Cloud.Firestore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace remoteLogIn
{
    public partial class Form1 : Form
    {
        string projectId = "remotelogin-33dbe";
        string uid;
        FirestoreDb db;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/config.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filepath);
            db = FirestoreDb.Create(projectId);
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("remoteLogIn");

            if (key.GetValue("uid") != null)
            {
                uid = key.GetValue("uid").ToString();
            }
            else
            {
                uid = Guid.NewGuid().ToString();
                key.SetValue("uid", uid);
            }
            key.Close();
            uidText.Text = uid;
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(uid, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            qrCodePictureBox.Image = qrCode.GetGraphic(80);
            checkForUpdates();
        }

        private async void checkForUpdates()
        {
            DocumentReference docRef = db.Collection("client").Document(uid);
            Dictionary<string, object> data = new Dictionary<string, object> { };
            await docRef.SetAsync(data);
            FirestoreChangeListener listener = docRef.Listen(async snapshot =>
            {
                if (snapshot.Exists)
                {
                    Dictionary<string, Object> snap = snapshot.ToDictionary();
                    if (!snap.ContainsKey("page") || !snap.ContainsKey("userName") || !snap.ContainsKey("password"))
                    {
                        return;
                    }
                    String page = snap["page"].ToString();
                    String userName = snap["userName"].ToString();
                    String password = snap["password"].ToString();
                    openPage(page, password, userName);
                    await docRef.SetAsync(data);
                }
            });
        }

        private async void openPage(String page, String password, String username)
        {
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            DocumentReference docRef = db.Collection("page").Document(page);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                Dictionary<string, Object> snap = snapshot.ToDictionary();
                ChromeOptions options = new ChromeOptions();
                options.AddArguments("start-maximized");
                try
                {
                    var driver = new ChromeDriver(driverService, options);
                    driver.Navigate().GoToUrl(snap["domain"].ToString());
                    driver.FindElement(By.Name(snap["userNameFieldID"].ToString())).SendKeys(username);
                    driver.FindElement(By.Name(snap["passwordFieldID"].ToString())).SendKeys(password);
                    driver.FindElement(By.Name(snap["submitButtonID"].ToString())).Click();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }


        }

    }
}
