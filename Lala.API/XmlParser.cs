﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.XPath;
using System.Reflection;
using System.Data;

namespace Lala.API
{
    public class XmlParser
    {
        public Stream GetXmlStream(string URL)
        {
            WebRequest request = WebRequest.Create(URL);
            request.Headers.Add("Cookie:" + API.Instance.Cookie);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }

        public XPathNavigator XPathNavFromStream(string URL)
        {
            Stream stream = GetXmlStream(URL);
            XPathDocument document = new XPathDocument(stream);
            return document.CreateNavigator();
        }

        public List<Object> SingleNodeCollection(Type typeToReturn, String xPath, XPathNavigator navigator)
        {
            XPathNodeIterator nodes = navigator.Select(xPath);
            List<Object> returnedList = new List<Object>(nodes.Count);
            while (nodes.MoveNext())
            {
                Object newObj = Activator.CreateInstance(typeToReturn);
                XPathNavigator nodesNavigator = nodes.Current;
                XPathNodeIterator nodesText = nodesNavigator.SelectDescendants(XPathNodeType.Element, false);
                System.Reflection.PropertyInfo pi = null;
                while (nodesText.MoveNext())
                {
                    try
                    {
                        pi = typeToReturn.GetProperty(nodesText.Current.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    }
                    catch (NullReferenceException ex)
                    {
                        continue;
                    }
                    //NEED TO WRITE IN AN ERROR LOG PARSER HERE. NullReferenceExcpetion will be thrown if the property sent from lala
                    //doesn't exist in our class. That needs to be caught and handled appropriately. //-WedTM
                    //Quick little hack for DateTime, since lala uses epoc for it's times...//-WedTM
                    try
                    {
                        if (pi.PropertyType == typeof(DateTime))
                            pi.SetValue(newObj, new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(nodesText.Current.ValueAsLong), null);
                        else
                            pi.SetValue(newObj, Convert.ChangeType(nodesText.Current.Value, pi.PropertyType), null);
                    }

                    catch (FormatException) // Catches null values for int type properties
                    {
                        pi.SetValue(newObj, Convert.ChangeType(0, pi.PropertyType), null);
                    }
                    catch (Exception ex)
                    {
                        //throw;
                        continue;
                    }
                }
                returnedList.Add(Convert.ChangeType(newObj, typeToReturn));
            }
            return returnedList;
        }
    }
}
