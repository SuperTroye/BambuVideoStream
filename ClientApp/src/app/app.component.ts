import { Component, OnInit } from '@angular/core';
import * as signalR from "@microsoft/signalr";
import { PrintMessage } from './models/PrintMessage';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {

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



}
