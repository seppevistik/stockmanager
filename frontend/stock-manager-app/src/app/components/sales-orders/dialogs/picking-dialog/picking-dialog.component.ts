import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { SalesOrderLine } from '../../../../models/sales-order.model';

export interface PickingDialogData {
  lines: SalesOrderLine[];
}

export interface PickedLineResult {
  lineId: number;
  quantityPicked: number;
  location?: string;
  notes?: string;
}

@Component({
  selector: 'app-picking-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatTableModule,
    MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>Complete Picking</h2>
    <mat-dialog-content>
      <p>Enter the quantities picked for each line item. You can pick partial quantities if needed.</p>
      <form [formGroup]="form">
        <div class="picking-table">
          <table>
            <thead>
              <tr>
                <th>Product</th>
                <th>Ordered</th>
                <th>Pick Qty</th>
                <th>Location</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody formArrayName="pickedLines">
              <tr *ngFor="let lineForm of pickedLines.controls; let i = index" [formGroupName]="i">
                <td>
                  <div class="product-info">
                    <strong>{{ data.lines[i].productSku }}</strong>
                    <div class="product-name">{{ data.lines[i].productName }}</div>
                  </div>
                </td>
                <td class="qty-cell">{{ data.lines[i].quantityOrdered }}</td>
                <td class="qty-cell">
                  <mat-form-field appearance="outline" class="compact-field">
                    <input matInput type="number" formControlName="quantityPicked"
                           min="0" [max]="data.lines[i].quantityOrdered" step="0.01">
                    <mat-error *ngIf="lineForm.get('quantityPicked')?.hasError('required')">
                      Required
                    </mat-error>
                    <mat-error *ngIf="lineForm.get('quantityPicked')?.hasError('min')">
                      Min 0
                    </mat-error>
                    <mat-error *ngIf="lineForm.get('quantityPicked')?.hasError('max')">
                      Max {{ data.lines[i].quantityOrdered }}
                    </mat-error>
                  </mat-form-field>
                </td>
                <td>
                  <mat-form-field appearance="outline" class="compact-field">
                    <input matInput formControlName="location" placeholder="e.g., A1-B2">
                  </mat-form-field>
                </td>
                <td>
                  <mat-form-field appearance="outline" class="compact-field">
                    <input matInput formControlName="notes" placeholder="Optional notes">
                  </mat-form-field>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="summary">
          <button mat-stroked-button type="button" (click)="fillAll()" style="margin-top: 16px;">
            <mat-icon>done_all</mat-icon>
            Fill All Ordered Quantities
          </button>
        </div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancel</button>
      <button mat-raised-button color="primary" (click)="onConfirm()" [disabled]="form.invalid">
        Complete Picking
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      min-width: 1000px;
      max-width: 1200px;
      max-height: 600px;
      padding: 20px 24px;
    }

    p {
      margin-bottom: 16px;
      color: rgba(0, 0, 0, 0.6);
      font-size: 0.9em;
    }

    .picking-table {
      overflow-x: auto;
      margin-bottom: 16px;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      table-layout: fixed;
    }

    th, td {
      padding: 8px 12px;
      text-align: left;
      border-bottom: 1px solid rgba(0, 0, 0, 0.12);
    }

    th {
      font-weight: 500;
      background-color: rgba(0, 0, 0, 0.04);
      font-size: 0.9em;
      white-space: nowrap;
    }

    th:nth-child(1) { width: 30%; }
    th:nth-child(2) { width: 10%; }
    th:nth-child(3) { width: 15%; }
    th:nth-child(4) { width: 20%; }
    th:nth-child(5) { width: 25%; }

    .product-info {
      display: flex;
      flex-direction: column;
    }

    .product-info strong {
      font-size: 0.9em;
    }

    .product-name {
      font-size: 0.8em;
      color: rgba(0, 0, 0, 0.6);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .qty-cell {
      text-align: center;
      font-size: 0.9em;
    }

    .compact-field {
      margin-bottom: -1.25em;
      font-size: 0.9em;
    }

    .compact-field ::ng-deep .mat-mdc-text-field-wrapper {
      padding: 0 !important;
    }

    .compact-field ::ng-deep .mat-mdc-form-field-infix {
      min-height: 40px !important;
      padding-top: 8px !important;
      padding-bottom: 8px !important;
    }

    .compact-field ::ng-deep .mat-mdc-form-field-subscript-wrapper {
      display: none;
    }

    .compact-field ::ng-deep input {
      font-size: 0.9em;
    }

    .summary {
      text-align: center;
      padding: 8px;
      background-color: rgba(0, 0, 0, 0.02);
      border-radius: 4px;
    }
  `]
})
export class PickingDialogComponent implements OnInit {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<PickingDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: PickingDialogData
  ) {
    this.form = this.fb.group({
      pickedLines: this.fb.array([])
    });
  }

  ngOnInit(): void {
    this.data.lines.forEach(line => {
      this.pickedLines.push(this.fb.group({
        lineId: [line.id],
        quantityPicked: [
          line.quantityOrdered,
          [
            Validators.required,
            Validators.min(0),
            Validators.max(line.quantityOrdered)
          ]
        ],
        location: [''],
        notes: ['']
      }));
    });
  }

  get pickedLines(): FormArray {
    return this.form.get('pickedLines') as FormArray;
  }

  fillAll(): void {
    this.data.lines.forEach((line, index) => {
      this.pickedLines.at(index).patchValue({
        quantityPicked: line.quantityOrdered
      });
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onConfirm(): void {
    if (this.form.valid) {
      const pickedLines: PickedLineResult[] = this.form.value.pickedLines.map((line: any) => ({
        lineId: line.lineId,
        quantityPicked: line.quantityPicked,
        location: line.location || undefined,
        notes: line.notes || undefined
      }));
      this.dialogRef.close({ pickedLines });
    }
  }
}
