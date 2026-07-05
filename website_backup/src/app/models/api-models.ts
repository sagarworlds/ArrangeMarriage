export interface User {
  userId: string;
  username: string;
  email: string;
  phoneNumber: string;
  role: 'Admin' | 'Bride' | 'Groom' | 'Staff' | 'Family';
  isVerified: boolean;
}

export interface Profile {
  profileId: string;
  userId: string;
  fullName: string;
  gender: string;
  dob: string;
  heightCm?: number;
  weightKg?: number;
  religion?: string;
  casteCommunity?: string;
  motherTongue?: string;
  maritalStatus: string;
  education?: string;
  occupation?: string;
  annualIncome?: number;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  smoking: boolean;
  drinking: boolean;
  diet?: string;
  hobbies?: string;
}

export interface Proposal {
  proposalId: string;
  senderId: string;
  receiverId: string;
  status: string;
  matchScore?: number;
  notes?: string;
}
