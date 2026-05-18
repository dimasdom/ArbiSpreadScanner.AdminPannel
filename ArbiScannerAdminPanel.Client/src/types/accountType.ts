// AdminAccountDTO
export interface AdminAccountDTO {
  token: string;
  accessToken: string;
  refreshToken: string;
  expiration: Date | string;
  roles: string[];
}

// AdminAccountAuthenticateDTO
export interface AdminAccountAuthenticateDTO {
  userName: string;
  password: string;
}

// ClientAccountDTO
export interface ClientAccountDTO {
  id: string;
  userMail: string;
  userName: string;
  subscription?: UserSubscriptionPayment | null;
  payments?: PaymentModel[] | null;
}

// ClientAccountTableRowDTO
export interface ClientAccountTableRowDTO {
  id: string;
  userMail: string;
  isActiveSubscription: boolean;
  subscriptionStartDate?: Date | string | null;
  subscriptionEndDate?: Date | string | null;
}

// UserSubscriptionModel
export interface UserSubscriptionModel {
  id: number;
  userId: string;
  subscriptionId: number;
  startDate: Date | string;
  endDate: Date | string;
  subscription?: SubscriptionModel | null;
}

// UserSubscriptionPayment
export interface UserSubscriptionPayment {
  id: number;
  userId: string;
  subscriptionId: number;
  paymentId: number;
  subscription?: SubscriptionModel | null;
  payment?: PaymentModel | null;
}

export const PaymentStatus = {
  Pending: 'Pending',
  Completed: 'Completed',
  Failed: 'Failed',
  Refunded: 'Refunded',
} as const;

export const getPaymentStatus = (status: number): string => {
  switch (status) {
    case 0:
      return PaymentStatus.Pending;
    case 1:
      return PaymentStatus.Completed;
    case 2:
      return PaymentStatus.Failed;
    case 3:
      return PaymentStatus.Refunded;
    default:
      return PaymentStatus.Pending;
  }
};

export type PaymentStatus = typeof PaymentStatus[keyof typeof PaymentStatus];

// SubscriptionModel
export interface SubscriptionModel {
  id: number;
  type: string;
  price: number;
  durationInDays: number;
}

export interface UserSubscriptionCreateDTO {
  userEmail: string;
  subscriptionId: number;
}

export interface UserSubscriptionRowDTO {
  id: number;
  userMail: string;
  subcriptionType: string;
  subscriptionStartDate: Date | string;
  subscriptionEndDate: Date | string;
}

// PaymentModel
export interface PaymentModel {
  id: number;
  userId: string;
  amount: number;
  paymentDate: Date | string;
  status: number;
  transactionId: string;
}

export interface PaymentResultDTO {
  id: number;
  userId: string;
  userEmail: string;
  amount: number;
  paymentUrl: string;
  paymentDate: Date | string;
  status: number;
  transactionId: string;
}

export const createEmptyAccountModel = (): AdminAccountDTO => ({
    token: '',
    expiration: new Date(),
});