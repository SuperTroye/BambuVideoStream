import { Component, OnInit } from '@angular/core';
import * as signalR from "@microsoft/signalr";
import { Ams, PrintMessage } from '../models/PrintMessage';


@Component({
  templateUrl: './sensors.component.html',
})
export class SensorsComponent implements OnInit {

  connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/signalr")
    .configureLogging(signalR.LogLevel.Information)
    .build();

  pm: PrintMessage = {} as PrintMessage;

  ngOnInit() {

    this.connection.on("SendPrintMessage", (p: PrintMessage) => {
    //console.log(p);
      this.pm = p;
    });

    this.start();
  }

  async start() {
    try {
      await this.connection.start();
      console.log("SignalR Connected.");
    } catch (err) {
      console.log(err);
      setTimeout(this.start, 5000);
    }
  }


  getFanSpeed(fanspeedvar: string) {

   //console.log(fanspeedvar);

    let fanSpeed = +fanspeedvar;
    let percent = (fanSpeed / 15);
    return percent;
  }


  getCurrentAmsTray(msg: Ams) {

    if (msg.tray_now !== undefined) {
      for (let ams of msg.ams) {
        for (let tray of ams.tray) {
          if (tray.id == msg.tray_now) {
            if (tray.tray_type == undefined || tray.tray_type == "") {
              tray.tray_type = "Empty"
            }
            return tray;
          }
        }
      }
    } else {
      return null;
    }

    return null;
  }



}
