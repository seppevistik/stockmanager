import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatChipsModule } from '@angular/material/chips';
import { FormsModule } from '@angular/forms';
import { CompanyService } from '../../../services/company.service';
import { Company } from '../../../models/company.model';

@Component({
  selector: 'app-companies-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatChipsModule,
    FormsModule
  ],
  templateUrl: './companies-list.component.html',
  styleUrl: './companies-list.component.scss'
})
export class CompaniesListComponent implements OnInit {
  companies: Company[] = [];
  filteredCompanies: Company[] = [];
  searchTerm: string = '';
  displayedColumns: string[] = ['name', 'contactPerson', 'email', 'phone', 'city', 'types', 'actions'];
  loading = false;

  constructor(
    private companyService: CompanyService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCompanies();
  }

  loadCompanies(): void {
    this.loading = true;
    this.companyService.getAll().subscribe({
      next: (companies) => {
        this.companies = companies;
        this.filteredCompanies = companies;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading companies:', error);
        this.loading = false;
      }
    });
  }

  filterCompanies(): void {
    const term = this.searchTerm.toLowerCase();
    this.filteredCompanies = this.companies.filter(c =>
      c.name.toLowerCase().includes(term) ||
      c.contactPerson?.toLowerCase().includes(term) ||
      c.email?.toLowerCase().includes(term) ||
      c.phone?.toLowerCase().includes(term) ||
      c.city?.toLowerCase().includes(term)
    );
  }

  addCompany(): void {
    this.router.navigate(['/companies/new']);
  }

  editCompany(id: number): void {
    this.router.navigate(['/companies/edit', id]);
  }

  deleteCompany(id: number, name: string): void {
    if (confirm(`Are you sure you want to delete ${name}?`)) {
      this.companyService.delete(id).subscribe({
        next: () => {
          this.loadCompanies();
        },
        error: (error) => {
          console.error('Error deleting company:', error);
          alert('Failed to delete company');
        }
      });
    }
  }
}
