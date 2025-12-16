import * as React from "react"
import { cn } from "@/lib/utils"
import styles from "./input.module.css"

export type InputProps = React.ComponentProps<"input"> & {
    label?: string
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
    ({ className, type, label, ...props }, ref) => {
        return (
            <div className={styles.container}>
                {label && <label className={styles.label}>{label}</label>}
                <div className={styles.inputWrapper}>
                    <input
                        type={type}
                        className={cn(styles.input, className)}
                        ref={ref}
                        {...props}
                    />
                </div>
            </div>
        )
    }
)
Input.displayName = "Input"

export { Input }
