#region File Information
/********************************************************************
  Project: ServiceBusMQ
  File:    MessageContentFormat.cs
  Created: 2013-01-26

  Author(s):
    Daniel Halan

 (C) Copyright 2013 Ingenious Technology with Quality Sweden AB
     all rights reserved

********************************************************************/
#endregion


namespace ServiceBusMQ.Manager {
  public enum MessageContentFormat { Xml, Json, Other, Unknown = 0xFF }
}
