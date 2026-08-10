import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { ToastService } from '../../shared/toast/toast.service';
import {
  AccountService,
  AddAddressRequest,
  Address,
  UpdateAddressRequest,
  UpdateUserProfileRequest,
  UserProfile,
} from './account.service';

@Component({
  selector: 'app-account',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './account.component.html',
  styleUrl: './account.component.scss',
})
export class AccountComponent implements OnInit {
  // FIX: thêm tab 'password' -- trước đây AuthService.changePassword() tồn tại ở
  // backend/FE service nhưng không có UI nào gọi tới, nên tính năng đổi mật khẩu
  // khi đang đăng nhập (không qua email) không thể dùng được trong thực tế.
  readonly activeTab = signal<'profile' | 'addresses' | 'password'>('profile');

  readonly profile = signal<UserProfile | null>(null);
  readonly addresses = signal<Address[]>([]);
  readonly loading = signal(false);
  readonly savingProfile = signal(false);
  readonly savingAddress = signal(false);
  readonly changingPassword = signal(false);

  profileForm: UpdateUserProfileRequest = { fullName: '', phone: '' };

  showAddressModal = false;
  editingAddressId: number | null = null;
  addressForm: AddAddressRequest = {
    recipientName: '',
    phone: '',
    detailAddress: '',
    ward: '',
    district: '',
    province: '',
    isDefault: false,
  };

  // --- Đổi mật khẩu ---
  passwordForm = { oldPassword: '', newPassword: '', confirmNewPassword: '' };
  readonly passwordPattern = '^(?=.*[A-Z])(?=.*\\d).{6,100}$';
  readonly passwordMaxLength = 100;
  passwordError = '';

  constructor(
    private readonly auth: AuthService,
    private readonly accountService: AccountService,
    private readonly toast: ToastService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadProfile();
    this.loadAddresses();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  private requireToken(): string | null {
    const token = this.getToken();
    if (!token) {
      this.router.navigate(['/auth/login']);
      return null;
    }
    return token;
  }

  loadProfile(): void {
    const token = this.requireToken();
    if (!token) return;
    this.loading.set(true);
    this.accountService.getProfile(token).subscribe({
      next: profile => {
        this.profile.set(profile);
        this.profileForm = { fullName: profile.fullName, phone: profile.phone ?? '' };
        this.loading.set(false);
      },
      error: () => {
        this.toast.error('Không thể tải thông tin tài khoản.');
        this.loading.set(false);
      },
    });
  }

  loadAddresses(): void {
    const token = this.getToken();
    if (!token) return;
    this.accountService.getAddresses(token).subscribe({
      next: addresses => this.addresses.set(addresses),
      error: () => this.toast.error('Không thể tải danh sách địa chỉ.'),
    });
  }

  submitProfile(): void {
    const token = this.requireToken();
    if (!token) return;
    this.savingProfile.set(true);
    this.accountService.updateProfile(token, this.profileForm).subscribe({
      next: profile => {
        this.profile.set(profile);
        this.savingProfile.set(false);
        this.toast.success('Cập nhật thông tin tài khoản thành công.');
      },
      error: err => {
        this.savingProfile.set(false);
        this.toast.error(err.error?.message ?? 'Lỗi cập nhật thông tin.');
      },
    });
  }

  openAddAddress(): void {
    this.editingAddressId = null;
    this.addressForm = {
      recipientName: '',
      phone: '',
      detailAddress: '',
      ward: '',
      district: '',
      province: '',
      isDefault: this.addresses().length === 0,
    };
    this.showAddressModal = true;
  }

  openEditAddress(addr: Address): void {
    this.editingAddressId = addr.id;
    this.addressForm = {
      recipientName: addr.recipientName,
      phone: addr.phone,
      detailAddress: addr.detailAddress,
      ward: addr.ward,
      district: addr.district,
      province: addr.province,
      isDefault: addr.isDefault,
    };
    this.showAddressModal = true;
  }

  closeAddressModal(): void {
    this.showAddressModal = false;
  }

  submitAddress(): void {
    const token = this.requireToken();
    if (!token) return;
    this.savingAddress.set(true);

    if (this.editingAddressId) {
      const req: UpdateAddressRequest = this.addressForm;
      this.accountService.updateAddress(token, this.editingAddressId, req).subscribe({
        next: () => {
          this.savingAddress.set(false);
          this.showAddressModal = false;
          this.toast.success('Cập nhật địa chỉ thành công.');
          this.loadAddresses();
        },
        error: err => {
          this.savingAddress.set(false);
          this.toast.error(err.error?.message ?? 'Lỗi cập nhật địa chỉ.');
        },
      });
    } else {
      this.accountService.addAddress(token, this.addressForm).subscribe({
        next: () => {
          this.savingAddress.set(false);
          this.showAddressModal = false;
          this.toast.success('Thêm địa chỉ thành công.');
          this.loadAddresses();
        },
        error: err => {
          this.savingAddress.set(false);
          this.toast.error(err.error?.message ?? 'Lỗi thêm địa chỉ.');
        },
      });
    }
  }

  deleteAddress(addr: Address): void {
    const token = this.requireToken();
    if (!token) return;
    if (!confirm(`Xóa địa chỉ "${addr.recipientName}"?`)) return;
    this.accountService.deleteAddress(token, addr.id).subscribe({
      next: () => {
        this.toast.success('Đã xóa địa chỉ.');
        this.loadAddresses();
      },
      error: err => this.toast.error(err.error?.message ?? 'Lỗi xóa địa chỉ.'),
    });
  }

  setDefault(addr: Address): void {
    const token = this.requireToken();
    if (!token) return;
    this.accountService.setDefaultAddress(token, addr.id).subscribe({
      next: () => {
        this.toast.success('Đã đặt làm địa chỉ mặc định.');
        this.loadAddresses();
      },
      error: err => this.toast.error(err.error?.message ?? 'Lỗi đặt địa chỉ mặc định.'),
    });
  }

  // --- Đổi mật khẩu ---
  submitPasswordChange(): void {
    this.passwordError = '';
    const token = this.requireToken();
    if (!token) return;

    const { oldPassword, newPassword, confirmNewPassword } = this.passwordForm;

    if (!oldPassword) {
      this.passwordError = 'Vui lòng nhập mật khẩu hiện tại.';
      return;
    }
    if (!newPassword || newPassword.length < 6) {
      this.passwordError = 'Mật khẩu mới phải có ít nhất 6 ký tự.';
      return;
    }
    if (newPassword.length > this.passwordMaxLength) {
      this.passwordError = `Mật khẩu mới không được vượt quá ${this.passwordMaxLength} ký tự.`;
      return;
    }
    if (!/[A-Z]/.test(newPassword)) {
      this.passwordError = 'Mật khẩu mới phải chứa ít nhất 1 chữ hoa.';
      return;
    }
    if (!/\d/.test(newPassword)) {
      this.passwordError = 'Mật khẩu mới phải chứa ít nhất 1 chữ số.';
      return;
    }
    if (newPassword !== confirmNewPassword) {
      this.passwordError = 'Xác nhận mật khẩu mới chưa khớp.';
      return;
    }

    this.changingPassword.set(true);
    this.auth.changePassword(token, { oldPassword, newPassword, confirmNewPassword }).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordForm = { oldPassword: '', newPassword: '', confirmNewPassword: '' };
        this.toast.success('Đổi mật khẩu thành công. Vui lòng đăng nhập lại.');
        // Backend đã thu hồi toàn bộ refresh token của user (kể cả phiên hiện tại),
        // nên đăng xuất luôn ở FE để đồng bộ trạng thái.
        this.auth.forceLogout();
        this.router.navigate(['/auth/login']);
      },
      error: err => {
        this.changingPassword.set(false);
        this.passwordError = err.error?.message ?? 'Đổi mật khẩu thất bại.';
      },
    });
  }
}