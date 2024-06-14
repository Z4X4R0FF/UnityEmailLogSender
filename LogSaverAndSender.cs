using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class LogSaverAndSender : MonoBehaviourSingletonPersistent<LogSaverAndSender>
{
    public bool enableSave = true;
    public bool enableMailing = true;

    public string yourEmail = "example@gmail.com";
    public string yourEmailPassword = "example";
    public string toEmail = "example@gmail.com";

    public void Init()
    {
    }

    [Serializable]
    public struct Logs
    {
        public string condition;
        public string stackTrace;
        public LogType type;

        public string dateTime;

        public Logs(string condition, string stackTrace, LogType type, string dateTime)
        {
            this.condition = condition;
            this.stackTrace = stackTrace;
            this.type = type;
            this.dateTime = dateTime;
        }
    }

    [Serializable]
    public class LogInfo
    {
        public List<Logs> logInfoList = new List<Logs>();
    }

    LogInfo logs = new LogInfo();

    void OnEnable()
    {
#if !UNITY_EDITOR
        //Email last saved log
        if (enableMailing)
        {
            mailLog();
        }
#endif

        //Subscribe to Log Event
        Application.logMessageReceived += LogCallback;
    }

    //Called when there is an exception
    void LogCallback(string condition, string stackTrace, LogType type)
    {
        //Create new Log
        Logs logInfo = new Logs(condition, stackTrace, type, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"));

        //Add it to the List
        logs.logInfoList.Add(logInfo);
    }

    void mailLog()
    {
        //Read old/last saved log
        LogInfo loadedData = DataSaver.loadData<LogInfo>("savelog");
        string date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

        //Send only if there is something to actually send
        if (loadedData != null && loadedData.logInfoList != null
                               && loadedData.logInfoList.Count > 0)
        {
            Debug.Log("Found log to send!");

            //Convert to json
            string messageToSend = JsonUtility.ToJson(loadedData, true);

            string attachmentPath = Path.Combine(Application.persistentDataPath, "data");
            attachmentPath = Path.Combine(attachmentPath, "savelog.txt");
            //Finally send email
            sendMail(yourEmail, yourEmailPassword, toEmail, "Log: " + date, messageToSend, attachmentPath);
            //Clear old log
            DataSaver.deleteData("savelog");
        }
    }

    void sendMail(string fromEmail, string emaiPassword, string toEmail, string eMailSubject, string eMailBody,
        string attachmentPath = null)
    {
        try
        {
            MailMessage mail = new MailMessage();

            mail.From = new MailAddress(fromEmail);
            mail.To.Add(toEmail);
            mail.Subject = eMailSubject;
            mail.Body = eMailBody;

            if (attachmentPath != null)
            {
                Attachment attachment = new System.Net.Mail.Attachment(attachmentPath);
                mail.Attachments.Add(attachment);
            }

            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Host = "smtp.gmail.com";
            smtpClient.Port = 587;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(fromEmail, emaiPassword);
            smtpClient.EnableSsl = true;
            ServicePointManager.ServerCertificateValidationCallback =
                delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };
            smtpClient.Send(mail);
        }
        catch (Exception e)
        {
        }
    }

    void OnDisable()
    {
        //Un-Subscribe from Log Event
        Application.logMessageReceived -= LogCallback;
    }

    //Save log  when focous is lost
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            //Save
            if (enableSave)
                DataSaver.saveData(logs, "savelog");
        }
    }

    //Save log on exit
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            //Save
            if (enableSave)
                DataSaver.saveData(logs, "savelog");
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("OnAppQuit log save");
        if (enableSave)
            DataSaver.saveData(logs, "savelog");
    }
}
