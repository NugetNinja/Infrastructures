﻿using Aiursoft.Handler.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aiursoft.Wrapgate.SDK.Models.ViewModels
{
    public class ViewMyRecordsViewModel : AiurProtocol
    {
        public string AppId { get; set; }
        public List<WrapRecord> Records { get; set; }
    }
}
