import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { UserSettingsComponent } from './user-settings/user-settings.component';
import { BusinessSettingsComponent } from './business-settings/business-settings.component';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    MatCardModule,
    MatIconModule,
    UserSettingsComponent,
    BusinessSettingsComponent
  ],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  isAdmin = false;
  isManagerOrAbove = false;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.hasRole([0]); // Admin
    this.isManagerOrAbove = this.authService.hasRole([0, 1]); // Admin or Manager
  }
}
