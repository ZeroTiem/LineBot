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

            if(inputText == "熱門話題")
            {
                var contents = _db.Contents
                    .Where(x => x.HotLevel > 1000)
                    .OrderByDescending(x => x.HotLevel)
                    .ToList();

                var num = (new Random()).Next(0, contents.Count);
                var content = contents.Skip(num).FirstOrDefault();

                card = new Card()
                {
                    Type = "buttons",
                    ThumbnailImageUrl = content.ImageUrl,
                    ImageAspectRatio = "rectangle",
                    ImageSize = "cover",
                    ImageBackgroundColor = "#FFFFFF",
                    Title = content.Title,
                    Text = content.Message.Substring(0, 60),
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
                                    Type = "message",
                                    Label = "下一則",
                                    Text = "下一則"
                                }
                            }
                };
                SendPushMessage(Card(userID, card));
                return Ok();
            }

            if(inputText == "隨機時事")
            {
                var max = _db.Contents.Count() - 1;
                var num = (new Random()).Next(0, max);

                var content = _db.Contents.OrderBy(x => x.HotLevel).Skip(num).FirstOrDefault();
                card = new Card()
                {
                    Type = "buttons",
                    ThumbnailImageUrl = content.ImageUrl,
                    ImageAspectRatio = "rectangle",
                    ImageSize = "cover",
                    ImageBackgroundColor = "#FFFFFF",
                    Title = content.Title,
                    Text = content.Message.Substring(0, 60),
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
                                    Type = "message",
                                    Label = "下一則",
                                    Text = "下一則"
                                }
                            }
                };
                SendPushMessage(Card(userID, card));
                return Ok();
            }

            if (inputText == "下一則")
            {
                var msgLog = _db.MessageLogs
                    .Where(x => x.UserId == userID)
                    .OrderByDescending(x => x.SendDatetime)
                    .FirstOrDefault();

                Content preContent = default(Content);
                CatContentMapping preMap = default(CatContentMapping);

                if(msgLog.MappingId == null)
                {
                    preContent = _db.Contents.FirstOrDefault(x => x.Id == msgLog.ContentId);
                    preMap = _db.CatContentMappings.FirstOrDefault(x => x.ContentId == preContent.Id);
                }
                else
                {
                    preMap = _db.CatContentMappings.FirstOrDefault(x => x.Id == msgLog.MappingId);
                    preContent = _db.Contents.FirstOrDefault(x => x.Id == preMap.ContentId);
                }
                //var contentID = msgLog.MappingId == null
                //    ? msgLog.ContentId
                //    : _db.CatContentMappings.FirstOrDefault(x => x.Id == msgLog.MappingId).ContentId;

                //var preContent = _db.Contents.FirstOrDefault(x => x.Id == contentID);

                //var nextContent = _db.Contents
                //    .Where(x => x.HotLevel < preContent.HotLevel)
                //    .OrderByDescending(x => x.HotLevel)
                //    .FirstOrDefault();
                var nextContent = _db.CatContentMappings
                    .Where(x => x.CategoryId == preMap.CategoryId)
                    .Join(_db.Contents,
                        m => m.ContentId,
                        c => c.Id,
                        (m, c) => c)
                    .FirstOrDefault(x => x.HotLevel < preContent.HotLevel);

                if (nextContent == null)
                {
                    var text = Text(userID, "你過度邊緣了\uDBC0\uDC840x100086");
                    SendPushMessage(text);
                    return Ok();
                }

                if (msgLog.MappingId == null)
                {
                    var nextMsgLog = new MessageLog()
                    {
                        UserId = userID,
                        ContentId = nextContent.Id,
                        KeyWord = msgLog.KeyWord
                    };
                    _db.MessageLogs.Add(nextMsgLog);
                    _db.SaveChanges();

                    card = new Card()
                    {
                        Type = "buttons",
                        ThumbnailImageUrl = nextContent.ImageUrl,
                        ImageAspectRatio = "rectangle",
                        ImageSize = "cover",
                        ImageBackgroundColor = "#FFFFFF",
                        Title = nextContent.Title,
                        Text = nextContent.Message.Substring(0, 60),
                        Actions = new List<ButtonAction>
                            {
                                new ButtonAction
                                {
                                    Type = "uri",
                                    Label = "前往",
                                    Uri = nextContent.Url
                                },
                                new ButtonAction
                                {
                                    Type = "message",
                                    Label = "下一則",
                                    Text = "下一則"
                                }
                            }
                    };
                }
                else
                {
                    var nextMsgLog = new MessageLog()
                    {
                        UserId = userID,
                        MappingId = nextContent.Id,
                        KeyWord = msgLog.KeyWord
                    };
                    _db.MessageLogs.Add(nextMsgLog);
                    _db.SaveChanges();

                    card = new Card()
                    {
                        Type = "buttons",
                        ThumbnailImageUrl = nextContent.ImageUrl,
                        ImageAspectRatio = "rectangle",
                        ImageSize = "cover",
                        ImageBackgroundColor = "#FFFFFF",
                        Title = nextContent.Title,
                        Text = nextContent.Message.Substring(0, 60),
                        Actions = new List<ButtonAction>
                            {
                                new ButtonAction
                                {
                                    Type = "uri",
                                    Label = "前往",
                                    Uri = nextContent.Url
                                },
                                new ButtonAction
                                {
                                    Type = "message",
                                    Label = "下一則",
                                    Text = "下一則"
                                }
                            }
                    };
                }
                SendPushMessage(Card(userID, card));
                return Ok();
            }

            if (cat != null)
            {
                var contentData = (from m in _db.CatContentMappings.Where(x => x.CategoryId == cat.Id)
                                   join c in _db.Contents
                                   on m.ContentId equals c.Id
                                   orderby c.HotLevel descending
                                   select new
                                   {
                                       Content = c,
                                       MappingID = m.Id
                                   })
                              .FirstOrDefault();
                if (contentData != null)
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
                        Text = content.Message.Substring(0, 60),
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
                                    Type = "message",
                                    Label = "下一則",
                                    Text = "下一則"
                                }
                            }
                    };
                    SendPushMessage(Card(userID, card));
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

                if (content != null)
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
                        Text = content.Message.Substring(0, 60),
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
                                    Type = "message",
                                    Label = "下一則",
                                    Text = "下一則"
                                }
                            }
                    };
                    SendPushMessage(Card(userID, card));
                }
                else
                {
                    isSuc = false;
                }

            }
            if (isSuc == false)
            {
                //todo
                SendPushMessage(GetFlexMenu(userID));
            }
            return Ok();
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

        private object GetFlexMenu(string to)
        {
            return new RequestSendPushMessage<dynamic>
            {
                To = to,
                Messages = new List<dynamic>
                        {
                            new
                            {
                                type = "flex",
                                altText = "this is a flex message",
                                contents = new
                                {
                                        type = "carousel",
                                        contents = new List <dynamic>
                                        {
                                            new
                                            {
                                                type = "bubble",
                                                body = new
                                                {
                                                    type="box",
                                                    layout = "vertical",
                                                    spacing = "sm",
                                                    contents = new List<dynamic>
                                                    {
                                                        new
                                                        {
                                                            type = "box",
                                                            layout = "horizontal",
                                                            spacing = "sm",
                                                            contents = new List<dynamic>
                                                            {
                                                                new
                                                                {
                                                                    type = "button",
                                                                    style = "primary",
                                                                    action = new
                                                                    {
                                                                        type = "message",
                                                                        label = "熱門話題",
                                                                        text = "熱門話題"
                                                                    }
                                                                },
                                                                new
                                                                {
                                                                    type = "button",
                                                                    style = "primary",
                                                                    action = new
                                                                    {
                                                                        type = "message",
                                                                        label = "隨機時事",
                                                                        text = "隨機時事"
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        ,
                                                        new
                                                        {
                                                            type = "box",
                                                            layout = "horizontal",
                                                            spacing = "sm",
                                                            contents = new List<dynamic>
                                                            {
                                                                new
                                                                {
                                                                    type = "button",
                                                                    style = "primary",
                                                                    action = new
                                                                    {
                                                                        type = "message",
                                                                        label = "運動",
                                                                        text = "運動"
                                                                    },
                                                                },
                                                                new
                                                                {
                                                                type = "button",
                                                                style = "primary",
                                                                action = new
                                                                {
                                                                    type = "message",
                                                                    label = "美食",
                                                                    text = "美食"
                                                                }
                                                            }
                                                            }
                                                        }
                                                        ,
                                                        new
                                                        {
                                                            type = "box",
                                                            layout = "horizontal",
                                                            spacing = "sm",
                                                            contents = new List<dynamic>
                                                            {
                                                                new
                                                                {
                                                                    type = "button",
                                                                    style = "primary",
                                                                    action = new
                                                                    {
                                                                        type = "message",
                                                                        label = "旅遊",
                                                                        text = "旅遊"
                                                                    }
                                                                },
                                                                new
                                                                {
                                                                    type = "button",
                                                                    style = "primary",
                                                                    action = new
                                                                    {
                                                                        type = "message",
                                                                        label = "男女",
                                                                        text = "男女"
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        ,
                                                        new
                                                        {
                                                            type = "box",
                                                            layout = "horizontal",
                                                            spacing = "sm",
                                                            contents = new List<dynamic>
                                                            {
                                                                new
                                                                {
                                                                    type = "button",
                                                                    style = "primary",
                                                                    action = new
                                                                    {
                                                                        type = "message",
                                                                        label = "電影",
                                                                        text = "電影"
                                                                    }
                                                                },
                                                                new
                                                                {
                                                                    type = "button",
                                                                    style = "primary",
                                                                    action = new
                                                                    {
                                                                        type = "message",
                                                                        label = "娛樂",
                                                                        text = "娛樂"
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                }
                            }
                        }

            };
        }

        private void SendPushMessage(object request)
        {
            //HttpClient Post
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ChannelAccessToken);
            var uri = "https://api.line.me/v2/bot/message/push";
            HttpResponseMessage httpResponseMessage;
            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                httpResponseMessage = client.PostAsync(uri, content).Result;
                string result = httpResponseMessage.Content.ReadAsStringAsync().Result;
                //Console.WriteLine(result);
                //response = JsonConvert.DeserializeObject<WebApiMemberRegisterRespone>(result);
            }
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
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }
}
