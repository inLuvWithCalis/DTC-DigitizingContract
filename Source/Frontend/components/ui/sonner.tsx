'use client'

import { useTheme } from 'next-themes'
import {
  toast as sonnerToast,
  Toaster as Sonner,
  type ToasterProps,
} from 'sonner'

const persistentError = ((message, options) =>
  sonnerToast.error(message, {
    ...options,
    duration: Infinity,
    closeButton: true,
  })) satisfies typeof sonnerToast.error

const toast = Object.assign(
  ((message, options) => sonnerToast(message, options)) as typeof sonnerToast,
  sonnerToast,
  { error: persistentError },
)

const Toaster = ({ ...props }: ToasterProps) => {
  const { theme = 'system' } = useTheme()

  return (
    <Sonner
      theme={theme as ToasterProps['theme']}
      className="toaster group"
      style={
        {
          '--normal-bg': 'var(--popover)',
          '--normal-text': 'var(--popover-foreground)',
          '--normal-border': 'var(--border)',
        } as React.CSSProperties
      }
      {...props}
    />
  )
}

export { Toaster, toast }
