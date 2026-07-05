-- Marriage Arranger Database Schema
-- Database: PostgreSQL

-- 1. Users Table
CREATE TYPE user_role AS ENUM ('Admin', 'Bride', 'Groom', 'Staff', 'Family');

CREATE TABLE users (
    user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    phone_number VARCHAR(20) UNIQUE NOT NULL,
    role user_role NOT NULL,
    is_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. Profiles Table
CREATE TYPE marital_status AS ENUM ('Never Married', 'Divorced', 'Widowed', 'Awaiting Divorce');

CREATE TABLE profiles (
    profile_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    full_name VARCHAR(100) NOT NULL,
    gender VARCHAR(10) NOT NULL,
    dob DATE NOT NULL,
    height_cm INT,
    weight_kg INT,
    religion VARCHAR(50),
    caste_community VARCHAR(50),
    mother_tongue VARCHAR(50),
    marital_status marital_status DEFAULT 'Never Married',
    education VARCHAR(100),
    occupation VARCHAR(100),
    annual_income DECIMAL(15, 2),
    address TEXT,
    city VARCHAR(50),
    state VARCHAR(50),
    country VARCHAR(50),
    smoking BOOLEAN DEFAULT FALSE,
    drinking BOOLEAN DEFAULT FALSE,
    diet VARCHAR(20),
    hobbies TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 3. Family Details Table
CREATE TABLE family_details (
    family_detail_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id UUID REFERENCES profiles(profile_id) ON DELETE CASCADE,
    father_name VARCHAR(100),
    mother_name VARCHAR(100),
    siblings_count INT,
    family_type VARCHAR(50), -- e.g., Nuclear, Joint
    family_business TEXT,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 4. Partner Preferences Table
CREATE TABLE partner_preferences (
    preference_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id UUID REFERENCES profiles(profile_id) ON DELETE CASCADE,
    min_age INT,
    max_age INT,
    min_height_cm INT,
    max_height_cm INT,
    preferred_education VARCHAR(100),
    preferred_religion VARCHAR(50),
    preferred_caste VARCHAR(50),
    preferred_location VARCHAR(100),
    min_income DECIMAL(15, 2),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 5. Documents Table
CREATE TABLE documents (
    document_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    document_type VARCHAR(50), -- e.g., 'Aadhaar', 'PAN', 'Education'
    file_url TEXT NOT NULL,
    is_verified BOOLEAN DEFAULT FALSE,
    verified_at TIMESTAMP WITH TIME ZONE,
    uploaded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 6. Membership Packages
CREATE TABLE membership_packages (
    package_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL, -- Basic, Premium, VIP
    description TEXT,
    price DECIMAL(10, 2) NOT NULL,
    duration_days INT NOT NULL,
    features JSONB, -- List of features
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 7. User Subscriptions
CREATE TABLE subscriptions (
    subscription_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    package_id UUID REFERENCES membership_packages(package_id),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    payment_status VARCHAR(20), -- Paid, Pending, Expired
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 8. Proposals Table
CREATE TYPE proposal_status AS ENUM ('Sent', 'Viewed', 'Accepted', 'Rejected', 'MeetingArranged', 'Finalized');

CREATE TABLE proposals (
    proposal_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sender_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    receiver_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    status proposal_status DEFAULT 'Sent',
    match_score INT,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 9. Payments Table
CREATE TYPE payment_type AS ENUM ('UPI', 'Card', 'NetBanking', 'Cash');
CREATE TYPE payment_status_type AS ENUM ('Paid', 'Pending', 'Overdue', 'Refunded');

CREATE TABLE payments (
    payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    amount DECIMAL(15, 2) NOT NULL,
    payment_type payment_type NOT NULL,
    status payment_status_type DEFAULT 'Pending',
    transaction_id VARCHAR(100),
    fee_type VARCHAR(50), -- Registration, Matchmaking, Success
    payment_date TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 10. Meetings Table
CREATE TYPE meeting_status AS ENUM ('Scheduled', 'Completed', 'Cancelled');

CREATE TABLE meetings (
    meeting_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    proposal_id UUID REFERENCES proposals(proposal_id) ON DELETE CASCADE,
    meeting_date TIMESTAMP WITH TIME ZONE NOT NULL,
    venue TEXT,
    is_online BOOLEAN DEFAULT FALSE,
    status meeting_status DEFAULT 'Scheduled',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 11. Meeting Feedback Table
CREATE TABLE meeting_feedback (
    feedback_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    meeting_id UUID REFERENCES meetings(meeting_id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    rating INT CHECK (rating >= 1 AND rating <= 5),
    comments TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 12. Marriage Success Tracking
CREATE TABLE marriage_successes (
    success_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bride_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    groom_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    proposal_id UUID REFERENCES proposals(proposal_id) ON DELETE CASCADE,
    engagement_date DATE,
    marriage_date DATE,
    success_fee_amount DECIMAL(15, 2),
    success_fee_status payment_status_type DEFAULT 'Pending',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Indices for performance
CREATE INDEX idx_profiles_gender ON profiles(gender);
CREATE INDEX idx_profiles_religion ON profiles(religion);
CREATE INDEX idx_profiles_caste ON profiles(caste_community);
CREATE INDEX idx_proposals_status ON proposals(status);
CREATE INDEX idx_payments_user ON payments(user_id);
CREATE INDEX idx_users_email ON users(email);
