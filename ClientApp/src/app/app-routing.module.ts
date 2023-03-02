import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FilesComponent } from './files/files.component';
import { SensorsComponent } from './sensors/sensors.component';

const routes: Routes = [
  { path: 'sensors', component: SensorsComponent },
  { path: 'files', component: FilesComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
