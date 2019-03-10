using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace TestTransactionScope
{
    class Program
    {
        static void Main(string[] args)
        {
            var publisher = new Publisher();
            var product = new Product("");
            Console.WriteLine("-- transaction is completed");
            using (var transaction = new TransactionScope())
            {
                product.Name = "Product A";
                publisher.PublishProduct(product);
                product.Name = "Product B";
                publisher.PublishProduct(product);
                Console.WriteLine("--> Commit");
                transaction.Complete();
            }

            Console.WriteLine();
            Console.WriteLine("-- transaction is not completed");
            using (var transaction = new TransactionScope())
            {
                publisher.PublishProduct(new Product("Product A"));
                publisher.PublishProduct(new Product("Product B"));
                Console.WriteLine("--> noCommit");
            }


            Console.WriteLine();
            Console.WriteLine("-- No transaction");
            {
                publisher.PublishProduct(new Product("Product A"));
                publisher.PublishProduct(new Product("Product B"));
                Console.WriteLine("--> end");
            }
            Console.ReadLine();

            Console.WriteLine(publisher);
        }
    }

    public class Publisher : PublisherBase
    {
        new class PublishData : PublisherBase.PublishData
        {
            public string TargetQueue { get; set; }
        }
        public void PublishProduct(Product product)
        {
            var data = new PublishData
            {
                SerializedContent = product.ToString(),
                TargetQueue = "Product"
            };
            ProcessPublish(data);
        }

        protected override void Publish(PublisherBase.PublishData data)
        {
            var localData = (PublishData)data;
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            try
            {
                Console.WriteLine(localData.TargetQueue + " - "+  localData.SerializedContent);
            }
            finally
            {
                Console.ForegroundColor = color;
            }
        }

    }

    public class Product
{

    public Product(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public abstract class PublisherBase
{
        protected class PublishData
        {
            public string SerializedContent { get; set; }
        }
    private Dictionary<Transaction, List<PublishData>> Transactions = new Dictionary<Transaction, List<PublishData>>();

        protected virtual void Publish(PublishData str)
        {
            Console.WriteLine(str);
        }


        protected void ProcessPublish(PublishData data)
        {
            if (Transaction.Current == null)
            {
                Publish(data);
            }
            else
            {
                lock (Transactions)
                {
                    if (!Transactions.ContainsKey(Transaction.Current))
                    {
                        Transactions[Transaction.Current] = new List<PublishData>();
                        Transaction.Current.TransactionCompleted += Current_TransactionCompleted;
                    }

                }
                Transactions[Transaction.Current].Add(data);
            }
        }


        private void Current_TransactionCompleted(object sender, TransactionEventArgs e)
        {
            lock (Transactions)
            {
                if (Transactions.ContainsKey(e.Transaction))
                {
                    try
                    {
                        if(e.Transaction.TransactionInformation.Status==TransactionStatus.Committed)
                            Transactions[e.Transaction].ForEach(s => Publish(s));
                    }
                    finally
                    {
                        Transactions.Remove(e.Transaction);
                    }
                }
            }
        }


        public override string ToString()
        {
            return $"{Transactions.Count()} transactions are in a pending state";
        }
    }
}
