import { Component, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardStats, StockSummary, RecentActivity, DailySalesData } from '../../models/dashboard.model';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

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
export class DashboardComponent implements OnInit, AfterViewInit {
  @ViewChild('salesCostsChart') salesCostsChart?: ElementRef<HTMLCanvasElement>;

  stats: DashboardStats | null = null;
  stockSummary: StockSummary | null = null;
  recentActivity: RecentActivity[] = [];
  salesCostsData: DailySalesData[] = [];
  loading = true;
  displayedColumns = ['productName', 'movementType', 'quantity', 'userName', 'createdAt'];

  private chart?: Chart;

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  ngAfterViewInit(): void {
    // Chart will be created after data is loaded
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

    this.dashboardService.getSalesCostsData(30).subscribe({
      next: (data) => {
        this.salesCostsData = data;
        this.createChart();
      },
      error: (error) => {
        console.error('Error loading sales/costs data:', error);
      }
    });
  }

  createChart(): void {
    if (!this.salesCostsChart || this.salesCostsData.length === 0) {
      return;
    }

    const ctx = this.salesCostsChart.nativeElement.getContext('2d');
    if (!ctx) return;

    // Destroy existing chart if it exists
    if (this.chart) {
      this.chart.destroy();
    }

    const labels = this.salesCostsData.map(d => {
      const date = new Date(d.date);
      return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    });

    const salesData = this.salesCostsData.map(d => d.sales);
    const costsData = this.salesCostsData.map(d => d.costs);

    this.chart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'Sales',
            data: salesData,
            backgroundColor: 'rgba(0, 108, 80, 0.7)',
            borderColor: 'rgba(0, 108, 80, 1)',
            borderWidth: 1,
            borderRadius: 6
          },
          {
            label: 'Costs',
            data: costsData,
            backgroundColor: 'rgba(186, 26, 26, 0.7)',
            borderColor: 'rgba(186, 26, 26, 1)',
            borderWidth: 1,
            borderRadius: 6
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: {
              font: {
                size: 13
              },
              padding: 20,
              usePointStyle: true,
              pointStyle: 'circle'
            }
          },
          tooltip: {
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            padding: 12,
            titleFont: {
              size: 13
            },
            bodyFont: {
              size: 13
            },
            callbacks: {
              label: function(context) {
                let label = context.dataset.label || '';
                if (label) {
                  label += ': ';
                }
                const value = context.parsed.y ?? 0;
                label += new Intl.NumberFormat('en-US', {
                  style: 'currency',
                  currency: 'USD'
                }).format(value);
                return label;
              }
            }
          }
        },
        scales: {
          x: {
            grid: {
              display: false
            },
            ticks: {
              font: {
                size: 11
              },
              maxRotation: 45,
              minRotation: 45
            }
          },
          y: {
            beginAtZero: true,
            grid: {
              color: 'rgba(0, 0, 0, 0.05)'
            },
            ticks: {
              font: {
                size: 11
              },
              callback: function(value) {
                return new Intl.NumberFormat('en-US', {
                  style: 'currency',
                  currency: 'USD',
                  minimumFractionDigits: 0,
                  maximumFractionDigits: 0
                }).format(value as number);
              }
            }
          }
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.chart) {
      this.chart.destroy();
    }
  }
}
