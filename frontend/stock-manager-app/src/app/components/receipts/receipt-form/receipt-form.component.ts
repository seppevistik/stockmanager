import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { ReceiptService } from '../../../services/receipt.service';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { PurchaseOrder } from '../../../models/purchase-order.model';
import { ItemCondition } from '../../../models/receipt.model';

@Component({
  selector: 'app-receipt-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule
  ],
  templateUrl: './receipt-form.component.html',
  styleUrl: './receipt-form.component.scss'
})
export class ReceiptFormComponent implements OnInit {
  receiptForm!: FormGroup;
  purchaseOrder?: PurchaseOrder;
  loading = false;
  saving = false;
  errorMessage = '';
  purchaseOrderId!: number;

  conditionOptions = [
    { value: ItemCondition.Good, label: 'Good' },
    { value: ItemCondition.Damaged, label: 'Damaged' },
    { value: ItemCondition.Defective, label: 'Defective' }
  ];

  displayedColumns: string[] = ['product', 'ordered', 'outstanding', 'receiving', 'condition'];

  constructor(
    private fb: FormBuilder,
    private receiptService: ReceiptService,
    private purchaseOrderService: PurchaseOrderService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    const poId = this.route.snapshot.queryParamMap.get('poId');
    if (!poId) {
      this.snackBar.open('Purchase order ID is required', 'Close', { duration: 3000 });
      this.router.navigate(['/purchase-orders']);
      return;
    }

    this.purchaseOrderId = +poId;
    this.loadPurchaseOrder(this.purchaseOrderId);
  }

  initForm(): void {
    this.receiptForm = this.fb.group({
      receiptDate: [new Date(), Validators.required],
      supplierDeliveryNote: [''],
      notes: [''],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.receiptForm.get('lines') as FormArray;
  }

  loadPurchaseOrder(id: number): void {
    this.loading = true;
    this.purchaseOrderService.getById(id).subscribe({
      next: (po) => {
        this.purchaseOrder = po;

        // Initialize line items
        this.lines.clear();
        po.lines.forEach(line => {
          if (line.quantityOutstanding > 0) {
            this.lines.push(this.fb.group({
              purchaseOrderLineId: [line.id],
              productId: [line.productId],
              productName: [line.productName],
              productSku: [line.productSku],
              quantityOrdered: [line.quantityOrdered],
              quantityOutstanding: [line.quantityOutstanding],
              quantityReceived: [line.quantityOutstanding, [Validators.required, Validators.min(0)]],
              unitPriceOrdered: [line.unitPrice],
              unitPriceReceived: [null],
              condition: [ItemCondition.Good, Validators.required],
              damageNotes: [''],
              location: [''],
              batchNumber: [''],
              expiryDate: [null]
            }));
          }
        });

        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading purchase order:', error);
        this.snackBar.open('Error loading purchase order', 'Close', { duration: 3000 });
        this.loading = false;
        this.router.navigate(['/purchase-orders']);
      }
    });
  }

  onConditionChange(index: number): void {
    const line = this.lines.at(index);
    const condition = line.get('condition')?.value;

    // If damaged or defective, show damage notes as required
    if (condition === ItemCondition.Damaged || condition === ItemCondition.Defective) {
      line.get('damageNotes')?.setValidators([Validators.required]);
    } else {
      line.get('damageNotes')?.clearValidators();
    }
    line.get('damageNotes')?.updateValueAndValidity();
  }

  onSubmit(): void {
    if (this.receiptForm.invalid) {
      this.errorMessage = 'Please fill in all required fields';
      return;
    }

    // Check if at least one item has quantity > 0
    const hasQuantity = this.lines.controls.some(line =>
      (line.get('quantityReceived')?.value || 0) > 0
    );

    if (!hasQuantity) {
      this.errorMessage = 'Please receive at least one item';
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    const formValue = this.receiptForm.value;
    const request = {
      purchaseOrderId: this.purchaseOrderId,
      receiptDate: formValue.receiptDate,
      supplierDeliveryNote: formValue.supplierDeliveryNote,
      notes: formValue.notes,
      lines: formValue.lines
        .filter((line: any) => line.quantityReceived > 0)
        .map((line: any) => ({
          purchaseOrderLineId: line.purchaseOrderLineId,
          quantityReceived: line.quantityReceived,
          unitPriceReceived: line.unitPriceReceived || undefined,
          condition: line.condition,
          damageNotes: line.damageNotes || undefined,
          location: line.location || undefined,
          batchNumber: line.batchNumber || undefined,
          expiryDate: line.expiryDate || undefined
        }))
    };

    this.receiptService.create(request).subscribe({
      next: (receipt) => {
        this.snackBar.open('Receipt created successfully', 'Close', { duration: 3000 });
        this.router.navigate(['/purchase-orders', this.purchaseOrderId]);
      },
      error: (error) => {
        console.error('Error creating receipt:', error);
        this.errorMessage = error.error?.message || 'Error creating receipt';
        this.saving = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/purchase-orders', this.purchaseOrderId]);
  }
}
