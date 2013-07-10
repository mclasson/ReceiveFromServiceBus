using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReceiveFromServiceBus
{
    public partial class Form1 : Form
    {
        delegate void SetText(Order order);
        public Form1()
        {
            InitializeComponent();
            ThreadStart ts = new ThreadStart(Runner);
            var t = new Thread(ts);
            t.Start();
        }
        void Runner()
        {
            string connectionString = "<your connectionstring here>";
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists("orderqueue"))
            {
                namespaceManager.CreateQueue("orderqueue");
            }

            var setText = new SetText((o) =>
            {
                var item = new ListViewItem("Order");
                item.SubItems.Add(o.Orderrows.Sum(i => i.Qty).ToString());
                item.SubItems.Add(o.CreatedOn.ToString("yyyy-MM-dd HH:ss"));
                listView1.Items.Add(item);
            });

            var Client = QueueClient.CreateFromConnectionString(connectionString, "orderqueue");
            while (true)
            {
                BrokeredMessage message = Client.Receive();
                if (message != null)
                {
                    try
                    {
                        var msgStream = message.GetBody<Stream>();
                        StreamReader sr = new StreamReader(msgStream);
                       
                        var order = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Order>(sr.ReadToEnd());
                        if (order != null)
                        {
                            order.CreatedOn = message.EnqueuedTimeUtc;
                            listView1.Invoke(setText, order);
                        }
                        message.Complete();
                    }
                    catch
                    {
                        message.DeadLetter();
                    }
                }

            }

        }
    }
    [DataContract]
    class Order
    {
        public DateTime CreatedOn { get; set; }
        public List<OrderRow> Orderrows { get; set; }
    }
    [CollectionDataContract]
    class OrderRow
    {
        [DataMember]
        public string Article { get; set; }
        [DataMember]
        public int Qty { get; set; }

    }
}
