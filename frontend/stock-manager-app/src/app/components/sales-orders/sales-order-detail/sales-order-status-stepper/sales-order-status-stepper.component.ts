import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SalesOrderStatus } from '../../../../models/sales-order.model';

interface StatusStep {
  status: SalesOrderStatus;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-sales-order-status-stepper',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatTooltipModule],
  template: `
    <div class="status-stepper">
      <div *ngFor="let step of steps; let i = index" class="step-container">
        <div class="step"
             [class.active]="isActive(step.status)"
             [class.completed]="isCompleted(step.status)"
             [class.cancelled]="currentStatus === cancelledStatus"
             [matTooltip]="step.label">
          <div class="step-circle">
            <mat-icon>{{ step.icon }}</mat-icon>
          </div>
          <div class="step-label">{{ step.label }}</div>
        </div>
        <div class="step-connector" *ngIf="i < steps.length - 1"
             [class.completed]="isCompleted(step.status)">
        </div>
      </div>
    </div>
  `,
  styles: [`
    .status-stepper {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 20px;
      background-color: #f5f5f5;
      border-radius: 8px;
      margin-bottom: 24px;
      overflow-x: auto;
    }

    .step-container {
      display: flex;
      align-items: center;
      flex: 1;
      min-width: 100px;
    }

    .step {
      display: flex;
      flex-direction: column;
      align-items: center;
      position: relative;
      flex: 0 0 auto;
    }

    .step-circle {
      width: 48px;
      height: 48px;
      border-radius: 50%;
      background-color: #e0e0e0;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.3s ease;
      margin-bottom: 8px;
    }

    .step.completed .step-circle {
      background-color: #4caf50;
      color: white;
    }

    .step.active .step-circle {
      background-color: #2196f3;
      color: white;
      box-shadow: 0 0 0 4px rgba(33, 150, 243, 0.2);
      transform: scale(1.1);
    }

    .step.cancelled .step-circle {
      background-color: #f44336;
      color: white;
    }

    .step-circle mat-icon {
      font-size: 24px;
      width: 24px;
      height: 24px;
    }

    .step-label {
      font-size: 12px;
      text-align: center;
      color: #666;
      white-space: nowrap;
    }

    .step.completed .step-label,
    .step.active .step-label {
      color: #333;
      font-weight: 500;
    }

    .step-connector {
      flex: 1;
      height: 2px;
      background-color: #e0e0e0;
      margin: 0 8px 32px 8px;
      transition: background-color 0.3s ease;
    }

    .step-connector.completed {
      background-color: #4caf50;
    }

    @media (max-width: 768px) {
      .status-stepper {
        overflow-x: scroll;
        justify-content: flex-start;
      }

      .step-container {
        min-width: 80px;
      }

      .step-circle {
        width: 40px;
        height: 40px;
      }

      .step-circle mat-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
      }
    }
  `]
})
export class SalesOrderStatusStepperComponent {
  @Input() currentStatus!: SalesOrderStatus;

  cancelledStatus = SalesOrderStatus.Cancelled;

  steps: StatusStep[] = [
    { status: SalesOrderStatus.Draft, label: 'Draft', icon: 'edit' },
    { status: SalesOrderStatus.Submitted, label: 'Submitted', icon: 'send' },
    { status: SalesOrderStatus.Confirmed, label: 'Confirmed', icon: 'check_circle' },
    { status: SalesOrderStatus.Picking, label: 'Picking', icon: 'playlist_add_check' },
    { status: SalesOrderStatus.Picked, label: 'Picked', icon: 'done_all' },
    { status: SalesOrderStatus.Packing, label: 'Packing', icon: 'inventory_2' },
    { status: SalesOrderStatus.Packed, label: 'Packed', icon: 'check_box' },
    { status: SalesOrderStatus.Shipped, label: 'Shipped', icon: 'local_shipping' },
    { status: SalesOrderStatus.Delivered, label: 'Delivered', icon: 'done' }
  ];

  isActive(status: SalesOrderStatus): boolean {
    return this.currentStatus === status;
  }

  isCompleted(status: SalesOrderStatus): boolean {
    // Special handling for cancelled orders
    if (this.currentStatus === SalesOrderStatus.Cancelled) {
      return false;
    }

    // Special handling for on-hold orders
    if (this.currentStatus === SalesOrderStatus.OnHold) {
      return false;
    }

    const currentIndex = this.steps.findIndex(s => s.status === this.currentStatus);
    const stepIndex = this.steps.findIndex(s => s.status === status);
    return stepIndex < currentIndex;
  }
}
