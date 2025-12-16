import Image from "next/image";
import Link from "next/link";
import { Button, ButtonVariant, ButtonSize } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import styles from "./login.module.css";

export default function LoginPage() {
  return (
    <div className={styles.page}>
      {/* Mobile-sized container: Fixed width on desktop to maintain aspect ratio */}
      <div className={styles.container}>

        {/* Header Section */}
        <div className={styles.header}>
          <div className={styles.logoRow}>
            <div className={styles.logoContainer}>
              <Image
                src="/assets/logo.png"
                alt="Hoppyfy Logo"
                fill
                className={styles.logoImage}
                priority
              />
            </div>
            <h1 className={styles.brandName}>oppyfy.me</h1>
          </div>
          <p className={styles.tagline}>The best stories start off-screen</p>
        </div>

        {/* Form Section */}
        <div className={styles.formSection}>
          <Input
            type="email"
            placeholder="Email"
            className={styles.inputEmail}
          />

          <div className={styles.passwordWrapper}>
            <Input
              type="password"
              placeholder="Password"
              label=""
            />
            <Link href="/forgot" className={styles.forgotLink}>
              Forgot?
            </Link>
          </div>
        </div>

        {/* Footer/Socials */}
        <div className={styles.footer}>
          <div className={styles.divider}>
            <span className={styles.dividerText}>Or continue with</span>
          </div>

          <div className={styles.socialGrid}>
            <Button variant={ButtonVariant.Social} size={ButtonSize.Xl} type="button">
              <Image src="/assets/google.png" alt="Google" width={24} height={24} priority />
              Google
            </Button>
            <Button variant={ButtonVariant.Social} size={ButtonSize.Xl} type="button">
              <Image src="/assets/facebook.png" alt="Facebook" width={24} height={24} priority />
              Facebook
            </Button>
          </div>
        </div>

      </div>
    </div>
  );
}
