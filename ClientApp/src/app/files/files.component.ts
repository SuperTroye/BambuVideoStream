import { Component, OnInit } from '@angular/core';
import { FilesService } from '../services/files.service';


@Component({
  templateUrl: './files.component.html'
})
export class FilesComponent implements OnInit {


  filesInCache: any[];

  constructor(private service: FilesService) {
  }


  ngOnInit() {
    this.service.listDirectory().subscribe(x => {
      this.filesInCache = x.filter(x => x.isDirectory === false);
    })
  }

}
