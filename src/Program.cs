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

            Console.WriteLine("-- transaction is completed");
            using (var transaction = new TransactionScope())
            {
                publisher.PublishProduct(new Product("Product A"));
                publisher.PublishProduct(new Product("Product B"));
                Console.WriteLine("--> Commit");
                transaction.Complete();
            }

            Console.WriteLine();
            Console.WriteLine("transaction is not completed");
            using (var transaction = new TransactionScope())
            {
                publisher.PublishProduct(new Product("Product A"));
                publisher.PublishProduct(new Product("Product B"));
                Console.WriteLine("--> Commit");
            }


            Console.WriteLine();
            Console.WriteLine("No transaction");
            {
                publisher.PublishProduct(new Product("Product A"));
                publisher.PublishProduct(new Product("Product B"));
                Console.WriteLine("--> Commit");
            }
            Console.ReadLine();

            Console.WriteLine(publisher);
        }
    }

    public class Publisher : PublisherBase
    {

        public void PublishProduct(Product product)
        {
            ProcessPublish(product);
        }

        protected override void Publish(string str)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            try
            {
                Console.WriteLine(str);
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
        private Dictionary<Transaction, List<string>> Transactions = new Dictionary<Transaction, List<string>>();

        protected virtual void Publish(string str)
        {
            Console.WriteLine(str);
        }


        protected void ProcessPublish(object product)
        {
            if (Transaction.Current == null)
            {
                Publish(product.ToString());
            }
            else
            {
                lock (Transactions)
                {
                    if (!Transactions.ContainsKey(Transaction.Current))
                    {
                        Transactions[Transaction.Current] = new List<string>();
                        Transaction.Current.TransactionCompleted += Current_TransactionCompleted;
                    }

                }
                Transactions[Transaction.Current].Add(product.ToString());
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
                            Transactions[e.Transaction].ForEach(s => Publish(s.ToString()));
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
