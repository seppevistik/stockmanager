import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardStats, StockSummary, RecentActivity } from '../../models/dashboard.model';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  stats: DashboardStats | null = null;
  stockSummary: StockSummary | null = null;
  recentActivity: RecentActivity[] = [];
  loading = true;
  displayedColumns = ['productName', 'movementType', 'quantity', 'userName', 'createdAt'];

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;

    this.dashboardService.getStats().subscribe({
      next: (stats) => {
        this.stats = stats;
      },
      error: (error) => {
        console.error('Error loading stats:', error);
      }
    });

    this.dashboardService.getStockSummary().subscribe({
      next: (summary) => {
        this.stockSummary = summary;
      },
      error: (error) => {
        console.error('Error loading stock summary:', error);
      }
    });

    this.dashboardService.getRecentActivity(10).subscribe({
      next: (activity) => {
        this.recentActivity = activity;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading recent activity:', error);
        this.loading = false;
      }
    });
  }
}
