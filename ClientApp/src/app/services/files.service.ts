import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";

@Injectable({
  providedIn: 'root'
})
export class FilesService {

  constructor(private http: HttpClient) {
  }

  listDirectory() {
    return this.http.get<any[]>("http://localhost:5000/api/print/listDirectory");
  }

}
