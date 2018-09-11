using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Xml;
using LineBot.Models;
using Newtonsoft.Json;

namespace LineBot.Controllers
{
    public class LineWebhookController : ApiController
    {
        public string ChannelAccessToken = ConfigurationManager.AppSettings["token"];
        private readonly ILineBotContext _db = new LineBotContext();

        public IHttpActionResult Get(string token)
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;
            return Ok();
        }

        [HttpPost]
        public IHttpActionResult Post()
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;
            var webhookEvent = JsonConvert.DeserializeObject<WebhookEvent>(jsonContent);
            var data = webhookEvent.Events.FirstOrDefault();
            var inputText = data.Message.Text;
            var userID = data.Source.UserId;
            var isSuc = true;
            Card card = default(Card);

            var cat = _db.Categories.FirstOrDefault(x => x.Title == inputText);
            if (cat != null)
            {
                var contentData = (from m in _db.CatContentMappings.Where(x => x.CategoryId == cat.Id)
                               join c in _db.Contents
                               on m.ContentId equals c.Id
                               orderby c.HotLevel descending
                               select new{
                                   Content = c,
                                   MappingID = m.Id
                               })
                              .FirstOrDefault();
                if(contentData != null)
                {
                    var msgLog = new MessageLog
                    {
                        UserId = userID,
                        MappingId = contentData.MappingID,
                        KeyWord = inputText
                    };
                    _db.MessageLogs.Add(msgLog);
                    _db.SaveChanges();

                    var content = contentData.Content;
                    card = new Card()
                    {
                        Type = "buttons",
                        ThumbnailImageUrl = content.ImageUrl,
                        ImageAspectRatio = "rectangle",
                        ImageSize = "cover",
                        ImageBackgroundColor = "#FFFFFF",
                        Title = content.Title,
                        Text = content.Message,
                        Actions = new List<ButtonAction>
                            {
                                new ButtonAction
                                {
                                    Type = "uri",
                                    Label = "前往",
                                    Uri = content.Url
                                },
                                new ButtonAction
                                {
                                    Type = "text",
                                    Label = "下一則",
                                    Data = "next"
                                }
                            }
                    };
                    Card(userID, card);
                }
                else
                {
                    isSuc = false;
                }
            }
            else
            {
                var content = _db.Contents
                    .Where(x => x.Title == inputText
                                || x.ContentKeyword.Contains(inputText))
                    .OrderByDescending(x => x.HotLevel)
                    .FirstOrDefault();

                if(content != null)
                {
                    var msgLog = new MessageLog
                    {
                        UserId = userID,
                        ContentId = content.Id,
                        KeyWord = inputText
                    };
                    _db.MessageLogs.Add(msgLog);
                    _db.SaveChanges();

                    card = new Card()
                    {
                        Type = "buttons",
                        ThumbnailImageUrl = content.ImageUrl,
                        ImageAspectRatio = "rectangle",
                        ImageSize = "cover",
                        ImageBackgroundColor = "#FFFFFF",
                        Title = content.Title,
                        Text = content.Message,
                        Actions = new List<ButtonAction>
                            {
                                new ButtonAction
                                {
                                    Type = "uri",
                                    Label = "前往",
                                    Uri = content.Url
                                },
                                new ButtonAction
                                {
                                    Type = "text",
                                    Label = "下一則",
                                    Data = "next"
                                }
                            }
                    };
                    Card(userID, card);
                }
                else
                {
                    isSuc = false;
                }
                
            }

            if(isSuc == false)
            {

            }

            //if (data.Postback != null)// Is Postback
            //{
            //    SendPushMessage("PostBackData:" + data.Postback.Data, data.Source.UserId);
            //}
            //if (data.Message.Text == "旋轉")
            //{
            //    SendPushMessage(data.Message.Text, data.Source.UserId);
            //}
            //else if (data.Message.Text == "卡片")
            //{
            //    SendPushMessage(data.Message.Text, data.Source.UserId);
            //}
            //else if (data.Message.Text == "f")
            //{
            //    SendReplyCardMessage(data.ReplyToken);
            //}
            //else
            //{
            //    SendReplyMessage(data.Message.Text, data.ReplyToken);
            //}
            return Ok();
        }

        private void SendReplyMessage(string text, string token)
        {
            var request = new RequestReplyMessage
            {
                ReplyToken = token,
                Messages = new List<TextMessage>
                {
                        new TextMessage {Text = text, Type = "text"}
                }

            };
            //HttpClient Post
            var client = new HttpClient();
            // Request head
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ChannelAccessToken);
            var uri = "https://api.line.me/v2/bot/message/reply";
            HttpResponseMessage httpResponseMessage;
            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));

            //var response = new WebApiMemberRegisterRespone();

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                httpResponseMessage = client.PostAsync(uri, content).Result;
                string result = httpResponseMessage.Content.ReadAsStringAsync().Result;
                //response = JsonConvert.DeserializeObject<WebApiMemberRegisterRespone>(result);
            }
        }

        private void SendReplyCardMessage(string token)
        {
            var request = new RequestReplyCardMessage
            {
                ReplyToken = token,
                Messages = new List<RequestTemplate<Card>>
                {
                    new RequestTemplate<Card>
                    {
                        Type = "template",
                        AltText = "This is a buttons template",
                        Template = new Card
                        {
                            Type = "buttons",
                            ThumbnailImageUrl = "https://example.com/bot/images/image.jpg",
                            ImageAspectRatio = "rectangle",
                            ImageSize = "cover",
                            ImageBackgroundColor = "#FFFFFF",
                            Title = "Menu",
                            Text = "Please select",
                            Actions = new List<ButtonAction>
                            {
                                new ButtonAction
                                {
                                    Type = "uri",
                                    Label = "label",
                                    Uri = "http://example.com/page/123"
                                },
                                new ButtonAction
                                {
                                    Type = "postback",
                                    Label = "postBack",
                                    Data = "action=add&itemid=123"
                                }
                            }
                        }
                    }
                }

            };
            //HttpClient Post
            var client = new HttpClient();
            // Request head
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ChannelAccessToken);
            var uri = "https://api.line.me/v2/bot/message/reply";
            HttpResponseMessage httpResponseMessage;
            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));

            //var response = new WebApiMemberRegisterRespone();

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                httpResponseMessage = client.PostAsync(uri, content).Result;
                string result = httpResponseMessage.Content.ReadAsStringAsync().Result;
                //response = JsonConvert.DeserializeObject<WebApiMemberRegisterRespone>(result);
            }
        }

        private void SendPushMessage(string text, string to)
        {
            object request = null;
            switch (text)
            {
                case "卡片":
                    //request = Card(to);
                    break;
                case "旋轉":
                    request = Carousel(to);
                    break;
                default:
                    request = Text(to, text);
                    break;
            }

            //HttpClient Post
            var client = new HttpClient();
            // Request head
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ChannelAccessToken);
            var uri = "https://api.line.me/v2/bot/message/push";
            HttpResponseMessage httpResponseMessage;
            // Request body
            var aa = JsonConvert.SerializeObject(request);
            //byte[] byteData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
            byte[] byteData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));

            //var response = new WebApiMemberRegisterRespone();

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                httpResponseMessage = client.PostAsync(uri, content).Result;
                string result = httpResponseMessage.Content.ReadAsStringAsync().Result;
                //response = JsonConvert.DeserializeObject<WebApiMemberRegisterRespone>(result);
            }
        }

        private object Text(string to, string text)
        {
            var request = new RequestSendPushMessage()
            {
                To = to,
                Messages = new List<TextMessage>
                {
                    new TextMessage {Text = text, Type = "text"}
                }

            };
            return request;
        }

        private object Card(string to, Card card)
        {
            var request = new RequestSendPushMessage<RequestTemplate<Card>>()
            {
                To = to,
                Messages = new List<RequestTemplate<Card>>
                {
                    new RequestTemplate<Card>
                    {
                        Type = "template",
                        AltText = "This is a buttons template",
                        Template = card
                    }
                }

            };
            return request;
        }

        private object Carousel(string to)
        {
            var request = new RequestSendPushMessage<RequestTemplate<Carousel>>()
            {
                To = to,
                Messages = new List<RequestTemplate<Carousel>>
                {
                    new RequestTemplate<Carousel>
                    {
                        Type = "template",
                        AltText = "This is a buttons template",
                        Template = new Carousel
                        {
                            Type = "carousel",
                            Cards = new List<CardBasis>
                            {
                                new Card
                                {
                                    Type = "buttons",
                                    ThumbnailImageUrl = "https://example.com/bot/images/image.jpg",
                                    ImageAspectRatio = "rectangle",
                                    ImageSize = "cover",
                                    ImageBackgroundColor = "#FFFFFF",
                                    Title = "Menu",
                                    Text = "Please select",
                                    //DefaultAction = new ButtonAction
                                    //{
                                    //    Type = "uri",
                                    //    Label = "label",
                                    //    Uri = "http://example.com/page/123"
                                    //},
                                    Actions = new List<ButtonAction>
                                    {
                                        new ButtonAction
                                        {
                                            Type = "uri",
                                            Label = "label",
                                            Uri = "http://example.com/page/123"
                                        },
                                        new ButtonAction
                                        {
                                            Type = "postback",
                                            Label = "postBack",
                                            Data = "action=add&itemid=123"
                                        }
                                    }
                                },
                                new Card
                                {
                                    Type = "buttons",
                                    ThumbnailImageUrl = "https://example.com/bot/images/image.jpg",
                                    ImageAspectRatio = "rectangle",
                                    ImageSize = "cover",
                                    ImageBackgroundColor = "#FFFFFF",
                                    Title = "Menu",
                                    Text = "Please select",
                                    //DefaultAction = new ButtonAction
                                    //{
                                    //    Type = "uri",
                                    //    Label = "label",
                                    //    Uri = "http://example.com/page/123"
                                    //},
                                    Actions = new List<ButtonAction>
                                    {
                                        new ButtonAction
                                        {
                                            Type = "uri",
                                            Label = "label",
                                            Uri = "http://example.com/page/123"
                                        },
                                        new ButtonAction
                                        {
                                            Type = "postback",
                                            Label = "postBack",
                                            Data = "action=add&itemid=123"
                                        }
                                    }
                                }
                            },
                            ImageAspectRatio = "rectangle",
                            ImageSize = "cover"
                        }
                    }
                }

            };
            return request;
        }




    }

    public class RequestReplyMessage
    {
        [JsonProperty(PropertyName = "replyToken")]
        public string ReplyToken { get; set; }
        [JsonProperty(PropertyName = "messages")]
        public List<TextMessage> Messages { get; set; }
    }

    public class RequestReplyCardMessage
    {
        [JsonProperty(PropertyName = "replyToken")]
        public string ReplyToken { get; set; }
        [JsonProperty(PropertyName = "messages")]
        public List<RequestTemplate<Card>> Messages { get; set; }
    }

    public class RequestSendPushMessage
    {
        [JsonProperty(PropertyName = "to")]
        public string To { get; set; }
        [JsonProperty(PropertyName = "messages")]
        public List<TextMessage> Messages { get; set; }
    }

    public class RequestSendPushMessage<T>
    {
        [JsonProperty(PropertyName = "to")]
        public string To { get; set; }
        [JsonProperty(PropertyName = "messages")]
        public List<T> Messages { get; set; }
    }

    public class TextMessage
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }


    public class WebhookEvent
    {
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        [JsonProperty(PropertyName = "replyToken")]
        public string ReplyToken { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "timestamp")]
        public string Timestamp { get; set; }
        [JsonProperty(PropertyName = "source")]
        public Source Source { get; set; }
        [JsonProperty(PropertyName = "message")]
        public LineMessage Message { get; set; }
        [JsonProperty(PropertyName = "postback")]
        public EventPostback Postback { get; set; }
    }


    public class EventPostback
    {
        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }
    }

    public class LineMessage
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }

    public class Source
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; }
    }

    //==================Buttons template=======================
    public class RequestTemplate<T>
    {
        [JsonProperty(PropertyName = "type")]
        public string Type = "template";
        [JsonProperty(PropertyName = "altText")]
        public string AltText { get; set; }
        [JsonProperty(PropertyName = "template")]
        public T Template { get; set; }
    }

    //public class RequestCardTemplate:CardTemplate
    //{
    //    [JsonProperty(PropertyName = "type")]
    //    public string Type { get; set; }
    //    [JsonProperty(PropertyName = "altText")]
    //    public string AltText { get; set; }
    //}

    //public class RequestCarouselTemplate
    //{
    //    [JsonProperty(PropertyName = "type")]
    //    public string Type { get; set; }
    //    [JsonProperty(PropertyName = "altText")]
    //    public string AltText { get; set; }
    //}

    //public class RequestCarouselTemplate
    //{
    //    [JsonProperty(PropertyName = "type")]
    //    public string Type = "carousel";
    //    [JsonProperty(PropertyName = "altText")]
    //    public string AltText { get; set; }
    //    [JsonProperty(PropertyName = "template")]
    //    public ButtonsTemplate Template { get; set; }
    //}

    //public class ButtonsTemplate<T>
    //{

    //}


    public class Card : CardBasis
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "imageAspectRatio")]
        public string ImageAspectRatio { get; set; }
        [JsonProperty(PropertyName = "imageSize")]
        public string ImageSize { get; set; }
    }

    public class Carousel
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "columns")]
        public List<CardBasis> Cards { get; set; }
        [JsonProperty(PropertyName = "imageAspectRatio")]
        public string ImageAspectRatio { get; set; }
        [JsonProperty(PropertyName = "imageSize")]
        public string ImageSize { get; set; }
    }

    public class CardBasis
    {
        [JsonProperty(PropertyName = "thumbnailImageUrl")]
        public string ThumbnailImageUrl { get; set; }
        [JsonProperty(PropertyName = "imageBackgroundColor")]
        public string ImageBackgroundColor { get; set; }
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
        //[JsonProperty(PropertyName = "defaultAction")]
        //public ButtonAction DefaultAction { get; set; }
        [JsonProperty(PropertyName = "actions")]
        public List<ButtonAction> Actions { get; set; }
    }



    //public class CardTemplate : Card
    //{
    //    [JsonProperty(PropertyName = "type")]
    //    public string Type { get; set; }
    //    [JsonProperty(PropertyName = "imageAspectRatio")]
    //    public string ImageAspectRatio { get; set; }
    //    [JsonProperty(PropertyName = "imageSize")]
    //    public string ImageSize { get; set; }
    //}


    public class ButtonAction
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "label")]
        public string Label { get; set; }
        //todo data or uri Boo
        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }
        [JsonProperty(PropertyName = "uri")]
        public string Uri { get; set; }
    }
}
